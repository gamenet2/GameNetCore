// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Contoso.GameNetCore.Proto;

namespace Contoso.GameNetCore.Routing
{
    /// <summary>
    /// Extension methods for <see cref="ProtoContext"/> related to routing.
    /// </summary>
    public static class RoutingProtoContextExtensions
    {
        /// <summary>
        /// Gets the <see cref="RouteData"/> associated with the provided <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="ProtoContext"/> associated with the current request.</param>
        /// <returns>The <see cref="RouteData"/>, or null.</returns>
        public static RouteData GetRouteData(this ProtoContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var routingFeature = httpContext.Features[typeof(IRoutingFeature)] as IRoutingFeature;
            return routingFeature?.RouteData;
        }

        /// <summary>
        /// Gets a route value from <see cref="RouteData.Values"/> associated with the provided
        /// <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="ProtoContext"/> associated with the current request.</param>
        /// <param name="key">The key of the route value.</param>
        /// <returns>The corresponding route value, or null.</returns>
        public static object GetRouteValue(this ProtoContext httpContext, string key)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var routingFeature = httpContext.Features[typeof(IRoutingFeature)] as IRoutingFeature;
            return routingFeature?.RouteData.Values[key];
        }
    }
}
