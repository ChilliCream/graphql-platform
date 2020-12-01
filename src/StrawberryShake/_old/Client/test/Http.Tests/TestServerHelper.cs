using System;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Http
{
    public static class TestServerHelper
    {
        public static IWebHost CreateServer(out int port)
        {
            for (int i = 5500; i < 6000; i++)
            {
                try
                {
                    var configBuilder = new ConfigurationBuilder();
                    configBuilder.AddInMemoryCollection();
                    var config = configBuilder.Build();
                    config["server.urls"] = $"http://localhost:{i}";

                    var host = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseKestrel()
                        .ConfigureServices(services => services.AddStarWars())
                        .Configure(app => app.UseWebSockets().UseGraphQL())
                        .Build();

                    host.Start();

                    port = i;
                    return host;
                }
                catch { }
            }

            throw new InvalidOperationException("Could not find a port.");
        }
    }
}
