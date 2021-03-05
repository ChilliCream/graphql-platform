using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.StarWars;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.WebSockets
{
    public static class TestServerHelper
    {
        public static IWebHost CreateServer(Action<IRequestExecutorBuilder> configure, out int port)
        {
            for (port = 5500; port < 6000; port++)
            {
                try
                {
                    var configBuilder = new ConfigurationBuilder();
                    configBuilder.AddInMemoryCollection();
                    var config = configBuilder.Build();
                    config["server.urls"] = $"http://localhost:{port}";
                    var host = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseKestrel()
                        .ConfigureServices(services =>
                        {
                            IRequestExecutorBuilder builder = services.AddRouting()
                                .AddGraphQLServer();

                            configure(builder);

                            builder
                                .AddStarWarsTypes()
                                .AddExportDirectiveType()
                                .AddStarWarsRepositories()
                                .AddInMemorySubscriptions();
                        })
                        .Configure(app =>
                            app.Use(async (ct, next) =>
                                {
                                    try
                                    {
                                        // Kestrel does not return proper error responses:
                                        // https://github.com/aspnet/KestrelHttpServer/issues/43
                                        await next();
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ct.Response.HasStarted)
                                        {
                                            throw;
                                        }

                                        ct.Response.StatusCode = 500;
                                        ct.Response.Headers.Clear();
                                        await ct.Response.WriteAsync(ex.ToString());
                                    }
                                })
                                .UseWebSockets()
                                .UseRouting()
                                .UseEndpoints(e => e.MapGraphQL()))
                        .Build();

                    host.Start();

                    return host;
                }
                catch { }
            }

            throw new InvalidOperationException("Not port found");
        }
    }
}
