// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using Contoso.GameNetCore.Builder;
using Contoso.GameNetCore.Hosting;
using Contoso.GameNetCore.Http;
using Contoso.GameNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;

namespace RewriteSample
{
    public class Startup
    {
        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public IWebHostEnvironment Environment { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RewriteOptions>(options =>
            {
                options.AddRedirect("(.*)/$", "$1")
                       .AddRewrite(@"app/(\d+)", "app?id=$1", skipRemainingRules: false)
                       .AddRedirectToHttps(302, 5001)
                       .AddIISUrlRewrite(Environment.ContentRootFileProvider, "UrlRewrite.xml")
                       .AddApacheModRewrite(Environment.ContentRootFileProvider, "Rewrite.txt");
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRewriter();

            app.Run(context =>
            {
                return context.Response.WriteAsync($"Rewritten Url: {context.Request.Path + context.Request.QueryString}");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 5000);
                    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                    {
                        // Configure SSL
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                    });
                })
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            host.Run();
        }
    }
}
