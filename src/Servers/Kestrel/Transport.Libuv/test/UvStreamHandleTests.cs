﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Contoso.GameNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Contoso.GameNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Contoso.GameNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Contoso.GameNetCore.Testing;
using Xunit;

namespace Contoso.GameNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class UvStreamHandleTests
    {
        [Fact]
        public void ReadStopIsIdempotent()
        {
            var libuvTrace = new LibuvTrace(new TestApplicationErrorLogger());

            using (var uvLoopHandle = new UvLoopHandle(libuvTrace))
            using (var uvTcpHandle = new UvTcpHandle(libuvTrace))
            {
                uvLoopHandle.Init(new MockLibuv());
                uvTcpHandle.Init(uvLoopHandle, null);

                UvStreamHandle uvStreamHandle = uvTcpHandle;
                uvStreamHandle.ReadStop();
                uvStreamHandle.ReadStop();
            }
        }
    }
}