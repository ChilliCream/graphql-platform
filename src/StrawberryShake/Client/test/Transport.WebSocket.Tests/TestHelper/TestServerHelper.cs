using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.StarWars;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace StrawberryShake.Transport.WebSockets;

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
                    .ConfigureServices(
                        services =>
                        {
                            var builder = services.AddRouting().AddGraphQLServer();

                            configure(builder);

                            builder
                                .AddStarWarsTypes()
                                .DisableIntrospection(disable: false)
                                .AddStarWarsRepositories()
                                .AddInMemorySubscriptions()
                                .ModifyOptions(
                                    o =>
                                    {
                                        o.EnableDefer = true;
                                        o.EnableStream = true;
                                    })
                                .UseDefaultPipeline()
                                .UseRequest(
                                    next => async context =>
                                    {
                                        if (context.ContextData.TryGetValue(
                                                nameof(HttpContext),
                                                out var value) &&
                                            value is HttpContext httpContext &&
                                            context.Result is HotChocolate.Execution.IOperationResult result)
                                        {
                                            var headers = httpContext.Request.Headers;
                                            if (headers.ContainsKey("sendErrorStatusCode"))
                                            {
                                                context.Result = result =
                                                    OperationResultBuilder
                                                        .FromResult(result)
                                                        .SetContextData(HttpStatusCode, 403)
                                                        .Build();
                                            }

                                            if (headers.ContainsKey("sendError"))
                                            {
                                                context.Result =
                                                    OperationResultBuilder
                                                        .FromResult(result)
                                                        .AddError(new Error("Some error!"))
                                                        .Build();
                                            }
                                        }

                                        await next(context);
                                    });
                        })
                    .Configure(
                        app =>
                            app.Use(
                                    async (ct, next) =>
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
            catch
            {
                // we ignore any errors here and try the next port
            }
        }

        throw new InvalidOperationException("Not port found");
    }
}
