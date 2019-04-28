// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Contoso.GameNetCore;
using Contoso.GameNetCore.Hosting.Server.Features;
using Contoso.GameNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StartRequestDelegateUrlApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            var messageSent = new ManualResetEventSlim(false);

            using (var host = WebHost.Start("http://127.0.0.1:0", async context =>
            {
                // Respond with the ApplicationName.
                var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
                await context.Response.WriteAsync(env.ApplicationName);
                messageSent.Set();
            }))
            {
                // Need these for test deployer to consider host deployment successful
                // The address written here is used by the client to send requests
                var addresses = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
                foreach (var address in addresses)
                {
                    Console.WriteLine($"Now listening on: {address}");
                }
                Console.WriteLine("Application started. Press Ctrl+C to shut down.");

                // Shut down after message sent or timeout
                messageSent.Wait(TimeSpan.FromSeconds(30));

            }
        }
    }
}
