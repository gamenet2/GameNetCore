using System;
using Contoso.GameNetCore.Builder;
using Contoso.GameNetCore.Hosting;
using Contoso.GameNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// HostingStartup's in the primary assembly are run automatically.
[assembly: HostingStartup(typeof(SampleStartups.StartupInjection))]

namespace SampleStartups
{
    public class StartupInjection : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseStartup<InjectedStartup>();
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                // Each of these three sets ApplicationName to the current assembly, which is needed in order to
                // scan the assembly for HostingStartupAttributes.
                // .UseSetting(WebHostDefaults.ApplicationKey, "SampleStartups")
                // .Configure(_ => { })
                .UseStartup<NormalStartup>()
                .Build();

            host.Run();
        }
    }

    public class NormalStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("NormalStartup.ConfigureServices");
        }

        public void Configure(IApplicationBuilder app)
        {
            Console.WriteLine("NormalStartup.Configure");
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }

    public class InjectedStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("InjectedStartup.ConfigureServices");
        }

        public void Configure(IApplicationBuilder app)
        {
            Console.WriteLine("InjectedStartup.Configure");
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
