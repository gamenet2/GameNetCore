// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Contoso.GameNetCore.Connections;
using Contoso.GameNetCore.Proto;
using Contoso.GameNetCore.Server.Kestrel.Core.Internal.Proto;
using Contoso.GameNetCore.Server.Kestrel.Core.Internal.Proto2.FlowControl;
using Contoso.GameNetCore.Server.Kestrel.Core.Internal.Proto2.HPack;
using Contoso.GameNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Contoso.GameNetCore.Server.Kestrel.Core.Internal.Proto2
{
    internal class Proto2FrameWriter
    {
        // Literal Header Field without Indexing - Indexed Name (Index 8 - :status)
        private static ReadOnlySpan<byte> _continueBytes => new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' };

        private readonly object _writeLock = new object();
        private readonly Proto2Frame _outgoingFrame;
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly PipeWriter _outputWriter;
        private readonly ConnectionContext _connectionContext;
        private readonly Proto2Connection _http2Connection;
        private readonly OutputFlowControl _connectionOutputFlowControl;
        private readonly string _connectionId;
        private readonly IKestrelTrace _log;
        private readonly ITimeoutControl _timeoutControl;
        private readonly MinDataRate _minResponseDataRate;
        private readonly TimingPipeFlusher _flusher;

        private uint _maxFrameSize = Proto2PeerSettings.MinAllowedMaxFrameSize;
        private byte[] _headerEncodingBuffer;
        private long _unflushedBytes;

        private bool _completed;
        private bool _aborted;

        public Proto2FrameWriter(
            PipeWriter outputPipeWriter,
            ConnectionContext connectionContext,
            Proto2Connection http2Connection,
            OutputFlowControl connectionOutputFlowControl,
            ITimeoutControl timeoutControl,
            MinDataRate minResponseDataRate,
            string connectionId,
            IKestrelTrace log)
        {
            _outputWriter = outputPipeWriter;
            _connectionContext = connectionContext;
            _http2Connection = http2Connection;
            _connectionOutputFlowControl = connectionOutputFlowControl;
            _connectionId = connectionId;
            _log = log;
            _timeoutControl = timeoutControl;
            _minResponseDataRate = minResponseDataRate;
            _flusher = new TimingPipeFlusher(_outputWriter, timeoutControl, log);
            _outgoingFrame = new Proto2Frame();
            _headerEncodingBuffer = new byte[_maxFrameSize];
        }

        public void UpdateMaxFrameSize(uint maxFrameSize)
        {
            lock (_writeLock)
            {
                if (_maxFrameSize != maxFrameSize)
                {
                    _maxFrameSize = maxFrameSize;
                    _headerEncodingBuffer = new byte[_maxFrameSize];
                }
            }
        }

        public void Complete()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                _connectionOutputFlowControl.Abort();
                _outputWriter.Complete();
            }
        }

        public void Abort(ConnectionAbortedException error)
        {
            lock (_writeLock)
            {
                if (_aborted)
                {
                    return;
                }

                _aborted = true;
                _connectionContext.Abort(error);

                Complete();
            }
        }

        public ValueTask<FlushResult> FlushAsync(IProtoOutputAborter outputAborter, CancellationToken cancellationToken)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }
                
                var bytesWritten = _unflushedBytes;
                _unflushedBytes = 0;

                return _flusher.FlushAsync(_minResponseDataRate, bytesWritten, outputAborter, cancellationToken);
            }
        }

        public ValueTask<FlushResult> Write100ContinueAsync(int streamId)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareHeaders(Proto2HeadersFrameFlags.END_HEADERS, streamId);
                _outgoingFrame.PayloadLength = _continueBytes.Length;
                WriteHeaderUnsynchronized();
                _outputWriter.Write(_continueBytes);
                return TimeFlushUnsynchronizedAsync();
            }
        }

        // Optional header fields for padding and priority are not implemented.
        /* https://tools.ietf.org/html/rfc7540#section-6.2
            +---------------+
            |Pad Length? (8)|
            +-+-------------+-----------------------------------------------+
            |E|                 Stream Dependency? (31)                     |
            +-+-------------+-----------------------------------------------+
            |  Weight? (8)  |
            +-+-------------+-----------------------------------------------+
            |                   Header Block Fragment (*)                 ...
            +---------------------------------------------------------------+
            |                           Padding (*)                       ...
            +---------------------------------------------------------------+
        */
        public void WriteResponseHeaders(int streamId, int statusCode, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                try
                {
                    _outgoingFrame.PrepareHeaders(Proto2HeadersFrameFlags.NONE, streamId);
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = _hpackEncoder.BeginEncode(statusCode, EnumerateHeaders(headers), buffer, out var payloadLength);
                    FinishWritingHeaders(streamId, payloadLength, done);
                }
                catch (HPackEncodingException hex)
                {
                    _log.HPackEncodingError(_connectionId, streamId, hex);
                    _http2Connection.Abort(new ConnectionAbortedException(hex.Message, hex));
                    throw new InvalidOperationException(hex.Message, hex); // Report the error to the user if this was the first write.
                }
            }
        }

        public ValueTask<FlushResult> WriteResponseTrailers(int streamId, ProtoResponseTrailers headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                try
                {
                    _outgoingFrame.PrepareHeaders(Proto2HeadersFrameFlags.END_STREAM, streamId);
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = _hpackEncoder.BeginEncode(EnumerateHeaders(headers), buffer, out var payloadLength);
                    FinishWritingHeaders(streamId, payloadLength, done);
                }
                catch (HPackEncodingException hex)
                {
                    _log.HPackEncodingError(_connectionId, streamId, hex);
                    _http2Connection.Abort(new ConnectionAbortedException(hex.Message, hex));
                }

                return TimeFlushUnsynchronizedAsync();
            }
        }

        private void FinishWritingHeaders(int streamId, int payloadLength, bool done)
        {
            var buffer = _headerEncodingBuffer.AsSpan();
            _outgoingFrame.PayloadLength = payloadLength;
            if (done)
            {
                _outgoingFrame.HeadersFlags |= Proto2HeadersFrameFlags.END_HEADERS;
            }

            WriteHeaderUnsynchronized();
            _outputWriter.Write(buffer.Slice(0, payloadLength));

            while (!done)
            {
                _outgoingFrame.PrepareContinuation(Proto2ContinuationFrameFlags.NONE, streamId);

                done = _hpackEncoder.Encode(buffer, out payloadLength);
                _outgoingFrame.PayloadLength = payloadLength;

                if (done)
                {
                    _outgoingFrame.ContinuationFlags = Proto2ContinuationFrameFlags.END_HEADERS;
                }

                WriteHeaderUnsynchronized();
                _outputWriter.Write(buffer.Slice(0, payloadLength));
            }
        }

        public ValueTask<FlushResult> WriteDataAsync(int streamId, StreamOutputFlowControl flowControl, ReadOnlySequence<byte> data, bool endStream)
        {
            // The Length property of a ReadOnlySequence can be expensive, so we cache the value.
            var dataLength = data.Length;

            lock (_writeLock)
            {
                if (_completed || flowControl.IsAborted)
                {
                    return default;
                }

                // Zero-length data frames are allowed to be sent immediately even if there is no space available in the flow control window.
                // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.1
                if (dataLength != 0 && dataLength > flowControl.Available)
                {
                    return WriteDataAsync(streamId, flowControl, data, dataLength, endStream);
                }

                // This cast is safe since if dataLength would overflow an int, it's guaranteed to be greater than the available flow control window.
                flowControl.Advance((int)dataLength);
                WriteDataUnsynchronized(streamId, data, dataLength, endStream);
                return TimeFlushUnsynchronizedAsync();
            }
        }

        /*  Padding is not implemented
            +---------------+
            |Pad Length? (8)|
            +---------------+-----------------------------------------------+
            |                            Data (*)                         ...
            +---------------------------------------------------------------+
            |                           Padding (*)                       ...
            +---------------------------------------------------------------+
        */
        private void WriteDataUnsynchronized(int streamId, ReadOnlySequence<byte> data, long dataLength, bool endStream)
        {
            // Note padding is not implemented
            _outgoingFrame.PrepareData(streamId);

            var dataPayloadLength = (int)_maxFrameSize; // Minus padding

            while (data.Length > dataPayloadLength)
            {
                var currentData = data.Slice(0, dataPayloadLength);
                _outgoingFrame.PayloadLength = dataPayloadLength; // Plus padding

                WriteHeaderUnsynchronized();

                foreach (var buffer in currentData)
                {
                    _outputWriter.Write(buffer.Span);
                }

                // Plus padding

                data = data.Slice(dataPayloadLength);
            }

            if (endStream)
            {
                _outgoingFrame.DataFlags |= Proto2DataFrameFlags.END_STREAM;
            }

            _outgoingFrame.PayloadLength = (int)data.Length; // Plus padding

            WriteHeaderUnsynchronized();

            foreach (var buffer in data)
            {
                _outputWriter.Write(buffer.Span);
            }

            // Plus padding
        }

        private async ValueTask<FlushResult> WriteDataAsync(int streamId, StreamOutputFlowControl flowControl, ReadOnlySequence<byte> data, long dataLength, bool endStream)
        {
            FlushResult flushResult = default;

            while (dataLength > 0)
            {
                OutputFlowControlAwaitable availabilityAwaitable;
                var writeTask = default(ValueTask<FlushResult>);

                lock (_writeLock)
                {
                    if (_completed || flowControl.IsAborted)
                    {
                        break;
                    }

                    var actual = flowControl.AdvanceUpToAndWait(dataLength, out availabilityAwaitable);

                    if (actual > 0)
                    {
                        if (actual < dataLength)
                        {
                            WriteDataUnsynchronized(streamId, data.Slice(0, actual), actual, endStream: false);
                            data = data.Slice(actual);
                            dataLength -= actual;
                        }
                        else
                        {
                            WriteDataUnsynchronized(streamId, data, actual, endStream);
                            dataLength = 0;
                        }

                        // Don't call TimeFlushUnsynchronizedAsync() since we time this write while also accounting for
                        // flow control induced backpressure below.
                        writeTask = _flusher.FlushAsync();
                    }

                    if (_minResponseDataRate != null)
                    {
                        _timeoutControl.BytesWrittenToBuffer(_minResponseDataRate, _unflushedBytes);
                    }

                    _unflushedBytes = 0;
                }

                // Avoid timing writes that are already complete. This is likely to happen during the last iteration.
                if (availabilityAwaitable == null && writeTask.IsCompleted)
                {
                    continue;
                }

                if (_minResponseDataRate != null)
                {
                    _timeoutControl.StartTimingWrite();
                }

                // This awaitable releases continuations in FIFO order when the window updates.
                // It should be very rare for a continuation to run without any availability.
                if (availabilityAwaitable != null)
                {
                    await availabilityAwaitable;
                }

                flushResult = await writeTask;

                if (_minResponseDataRate != null)
                {
                    _timeoutControl.StopTimingWrite();
                }
            }

            // Ensure that the application continuation isn't executed inline by ProcessWindowUpdateFrameAsync.
            await ThreadPoolAwaitable.Instance;

            return flushResult;
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.9
            +-+-------------------------------------------------------------+
            |R|              Window Size Increment (31)                     |
            +-+-------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WriteWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareWindowUpdate(streamId, sizeIncrement);
                WriteHeaderUnsynchronized();
                var buffer = _outputWriter.GetSpan(4);
                Bitshifter.WriteUInt31BigEndian(buffer, (uint)sizeIncrement, preserveHighestBit: false);
                _outputWriter.Advance(4);
                return TimeFlushUnsynchronizedAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.4
            +---------------------------------------------------------------+
            |                        Error Code (32)                        |
            +---------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WriteRstStreamAsync(int streamId, Proto2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareRstStream(streamId, errorCode);
                WriteHeaderUnsynchronized();
                var buffer = _outputWriter.GetSpan(4);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)errorCode);
                _outputWriter.Advance(4);

                return TimeFlushUnsynchronizedAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.5.1
            List of:
            +-------------------------------+
            |       Identifier (16)         |
            +-------------------------------+-------------------------------+
            |                        Value (32)                             |
            +---------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WriteSettingsAsync(IList<Proto2PeerSetting> settings)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareSettings(Proto2SettingsFrameFlags.NONE);
                var settingsSize = settings.Count * Proto2FrameReader.SettingSize;
                _outgoingFrame.PayloadLength = settingsSize;
                WriteHeaderUnsynchronized();

                var buffer = _outputWriter.GetSpan(settingsSize).Slice(0, settingsSize); // GetSpan isn't precise
                WriteSettings(settings, buffer);
                _outputWriter.Advance(settingsSize);

                return TimeFlushUnsynchronizedAsync();
            }
        }

        internal static void WriteSettings(IList<Proto2PeerSetting> settings, Span<byte> destination)
        {
            foreach (var setting in settings)
            {
                BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)setting.Parameter);
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(2), setting.Value);
                destination = destination.Slice(Proto2FrameReader.SettingSize);
            }
        }

        // No payload
        public ValueTask<FlushResult> WriteSettingsAckAsync()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareSettings(Proto2SettingsFrameFlags.ACK);
                WriteHeaderUnsynchronized();
                return TimeFlushUnsynchronizedAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.7
            +---------------------------------------------------------------+
            |                                                               |
            |                      Opaque Data (64)                         |
            |                                                               |
            +---------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WritePingAsync(Proto2PingFrameFlags flags, ReadOnlySequence<byte> payload)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PreparePing(Proto2PingFrameFlags.ACK);
                Debug.Assert(payload.Length == _outgoingFrame.PayloadLength); // 8
                WriteHeaderUnsynchronized();
                foreach (var segment in payload)
                {
                    _outputWriter.Write(segment.Span);
                }

                return TimeFlushUnsynchronizedAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.8
            +-+-------------------------------------------------------------+
            |R|                  Last-Stream-ID (31)                        |
            +-+-------------------------------------------------------------+
            |                      Error Code (32)                          |
            +---------------------------------------------------------------+
            |                  Additional Debug Data (*)                    | (not implemented)
            +---------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WriteGoAwayAsync(int lastStreamId, Proto2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareGoAway(lastStreamId, errorCode);
                WriteHeaderUnsynchronized();

                var buffer = _outputWriter.GetSpan(8);
                Bitshifter.WriteUInt31BigEndian(buffer, (uint)lastStreamId, preserveHighestBit: false);
                buffer = buffer.Slice(4);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)errorCode);
                _outputWriter.Advance(8);

                return TimeFlushUnsynchronizedAsync();
            }
        }

        private void WriteHeaderUnsynchronized()
        {
            _log.Proto2FrameSending(_connectionId, _outgoingFrame);
            WriteHeader(_outgoingFrame, _outputWriter);

            // We assume the payload will be written prior to the next flush.
            _unflushedBytes += Proto2FrameReader.HeaderLength + _outgoingFrame.PayloadLength;
        }

        /* https://tools.ietf.org/html/rfc7540#section-4.1
            +-----------------------------------------------+
            |                 Length (24)                   |
            +---------------+---------------+---------------+
            |   Type (8)    |   Flags (8)   |
            +-+-------------+---------------+-------------------------------+
            |R|                 Stream Identifier (31)                      |
            +=+=============================================================+
            |                   Frame Payload (0...)                      ...
            +---------------------------------------------------------------+
        */
        internal static void WriteHeader(Proto2Frame frame, PipeWriter output)
        {
            var buffer = output.GetSpan(Proto2FrameReader.HeaderLength);

            Bitshifter.WriteUInt24BigEndian(buffer, (uint)frame.PayloadLength);
            buffer = buffer.Slice(3);

            buffer[0] = (byte)frame.Type;
            buffer[1] = frame.Flags;
            buffer = buffer.Slice(2);

            Bitshifter.WriteUInt31BigEndian(buffer, (uint)frame.StreamId, preserveHighestBit: false);

            output.Advance(Proto2FrameReader.HeaderLength);
        }

        private ValueTask<FlushResult> TimeFlushUnsynchronizedAsync()
        {
            var bytesWritten = _unflushedBytes;
            _unflushedBytes = 0;

            return _flusher.FlushAsync(_minResponseDataRate, bytesWritten);
        }

        public bool TryUpdateConnectionWindow(int bytes)
        {
            lock (_writeLock)
            {
                return _connectionOutputFlowControl.TryUpdateWindow(bytes);
            }
        }

        public bool TryUpdateStreamWindow(StreamOutputFlowControl flowControl, int bytes)
        {
            lock (_writeLock)
            {
                return flowControl.TryUpdateWindow(bytes);
            }
        }

        public void AbortPendingStreamDataWrites(StreamOutputFlowControl flowControl)
        {
            lock (_writeLock)
            {
                flowControl.Abort();
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumerateHeaders(IHeaderDictionary headers)
        {
            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    yield return new KeyValuePair<string, string>(header.Key, value);
                }
            }
        }
    }
}
