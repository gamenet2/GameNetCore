// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETX
using Microsoft.AspNetCore.Builder;
#else
using Contoso.GameNetCore.Builder;
#endif
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Contoso.GameNetCore.Hosting
{
    public abstract class StartupBase : IStartup
    {
        public abstract void Configure(IApplicationBuilder app);

        IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
        {
            ConfigureServices(services);
            return CreateServiceProvider(services);
        }

        public virtual void ConfigureServices(IServiceCollection services) { }

        public virtual IServiceProvider CreateServiceProvider(IServiceCollection services) =>
            services.BuildServiceProvider();
    }

    public abstract class StartupBase<TBuilder> : StartupBase
    {
        readonly IServiceProviderFactory<TBuilder> _factory;

        public StartupBase(IServiceProviderFactory<TBuilder> factory) =>
            _factory = factory;

        public override IServiceProvider CreateServiceProvider(IServiceCollection services)
        {
            var builder = _factory.CreateBuilder(services);
            ConfigureContainer(builder);
            return _factory.CreateServiceProvider(builder);
        }

        public virtual void ConfigureContainer(TBuilder builder) { }
    }
}