// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;

namespace Contoso.GameNetCore.Routing.Matching
{
    public class RouteEndpointAzureBenchmark : MatcherAzureBenchmarkBase
    {
        [Benchmark]
        public void CreateEndpoints()
        {
            SetupEndpoints();
        }
    }
}