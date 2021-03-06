// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Pipelines
{
    public class StreamPipeReaderAdapterOptions
    {
        public static StreamPipeReaderAdapterOptions DefaultOptions = new StreamPipeReaderAdapterOptions();
        public const int DefaultMinimumSegmentSize = 4096;
        public const int DefaultMinimumReadThreshold = 256;

        public StreamPipeReaderAdapterOptions()
        {
        }

        public StreamPipeReaderAdapterOptions(int minimumSegmentSize, int minimumReadThreshold, MemoryPool<byte> memoryPool)
        {
            MinimumSegmentSize = minimumSegmentSize;
            MinimumReadThreshold = minimumReadThreshold;
            MemoryPool = memoryPool;
        }

        public int MinimumSegmentSize { get; set; } = DefaultMinimumSegmentSize;

        public int MinimumReadThreshold { get; set; } = DefaultMinimumReadThreshold;

        public MemoryPool<byte> MemoryPool { get; set; } = MemoryPool<byte>.Shared;
    }
}
