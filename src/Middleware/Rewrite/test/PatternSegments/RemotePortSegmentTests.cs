﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Contoso.GameNetCore.Http;
using Contoso.GameNetCore.Rewrite.Internal.PatternSegments;
using Xunit;

namespace Contoso.GameNetCore.Rewrite.Tests.PatternSegments
{
    public class RemotePortSegmentTests
    {
        [Fact]
        public void RemotePort_AssertSegmentIsCorrect()
        {
            // Arrange
            var segement = new RemotePortSegment();
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            context.HttpContext.Connection.RemotePort = 800;
            // Act
            var results = segement.Evaluate(context, null, null);

            // Assert
            Assert.Equal("800", results);
        }
    }
}
