﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Contoso.GameNetCore.Rewrite.Internal.PatternSegments
{
    public class IsHttpsModSegment : PatternSegment
    {
        // Note: Mod rewrite pattern matches on lower case "on" and "off"
        // while IIS looks for capitalized "ON" and "OFF"
        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            return context.HttpContext.Request.IsHttps ? "on" : "off";
        }
    }
}
