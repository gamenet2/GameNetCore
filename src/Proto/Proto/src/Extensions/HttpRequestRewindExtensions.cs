// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Contoso.GameNetCore.Proto.Internal;

namespace Contoso.GameNetCore.Proto
{
    /// <summary>
    /// Extension methods for enabling buffering in an <see cref="ProtoRequest"/>.
    /// </summary>
    public static class ProtoRequestRewindExtensions
    {
        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="ProtoRequest.Body"/> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than 30K bytes to disk.
        /// </summary>
        /// <param name="request">The <see cref="ProtoRequest"/> to prepare.</param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public static void EnableBuffering(this ProtoRequest request)
        {
            BufferingHelper.EnableRewind(request);
        }

        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="ProtoRequest.Body"/> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than <paramref name="bufferThreshold"/> bytes to
        /// disk.
        /// </summary>
        /// <param name="request">The <see cref="ProtoRequest"/> to prepare.</param>
        /// <param name="bufferThreshold">
        /// The maximum size in bytes of the in-memory <see cref="System.Buffers.ArrayPool{Byte}"/> used to buffer the
        /// stream. Larger request bodies are written to disk.
        /// </param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public static void EnableBuffering(this ProtoRequest request, int bufferThreshold)
        {
            BufferingHelper.EnableRewind(request, bufferThreshold);
        }

        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="ProtoRequest.Body"/> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than 30K bytes to disk.
        /// </summary>
        /// <param name="request">The <see cref="ProtoRequest"/> to prepare.</param>
        /// <param name="bufferLimit">
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
        /// <see cref="System.IO.IOException"/>.
        /// </param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public static void EnableBuffering(this ProtoRequest request, long bufferLimit)
        {
            BufferingHelper.EnableRewind(request, bufferLimit: bufferLimit);
        }

        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="ProtoRequest.Body"/> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than <paramref name="bufferThreshold"/> bytes to
        /// disk.
        /// </summary>
        /// <param name="request">The <see cref="ProtoRequest"/> to prepare.</param>
        /// <param name="bufferThreshold">
        /// The maximum size in bytes of the in-memory <see cref="System.Buffers.ArrayPool{Byte}"/> used to buffer the
        /// stream. Larger request bodies are written to disk.
        /// </param>
        /// <param name="bufferLimit">
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
        /// <see cref="System.IO.IOException"/>.
        /// </param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public static void EnableBuffering(this ProtoRequest request, int bufferThreshold, long bufferLimit)
        {
            BufferingHelper.EnableRewind(request, bufferThreshold, bufferLimit);
        }
    }
}
