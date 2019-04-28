﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Contoso.GameNetCore.ResponseCaching;

namespace Contoso.GameNetCore.Builder
{
    public static class ResponseCachingExtensions
    {
        public static IApplicationBuilder UseResponseCaching(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ResponseCachingMiddleware>();
        }
    }
}
