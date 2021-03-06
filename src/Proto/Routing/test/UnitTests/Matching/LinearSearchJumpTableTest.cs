﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Contoso.GameNetCore.Routing.Matching
{
    public class LinearSearchJumpTableTest : MultipleEntryJumpTableTest
    {
        internal override JumpTable CreateTable(
            int defaultDestination,
            int existDestination,
            params (string text, int destination)[] entries)
        {
            return new LinearSearchJumpTable(defaultDestination, existDestination, entries);
        }
    }
}
