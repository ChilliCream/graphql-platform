using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.StarWars;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StrawberryShake.Transport.WebSockets;

public static class TestServerHelper
{
    public static WebApplication CreateServer(Action<IRequestExecutorBuilder> configure, out int port)
    {
        for (port = 5500; port < 6000; port++)
        {
            try
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.WebHost.UseKestrel();
                builder.WebHost.UseUrls($"http://localhost:{port}");

                var gqlBuilder = builder.Services.AddRouting().AddGraphQLServer();

                configure(gqlBuilder);

                gqlBuilder
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
                                    out var value)
                                && value is HttpContext httpContext
                                && context.Result is OperationResult result)
                            {
                                var headers = httpContext.Request.Headers;
                                if (headers.ContainsKey("sendErrorStatusCode"))
                                {
                                    result.ContextData =
                                        result.ContextData.SetItem(
                                            ExecutionContextData.HttpStatusCode,
                                            403);
                                }

                                if (headers.ContainsKey("sendError"))
                                {
                                    result.Errors = result.Errors.Add(
                                        new Error { Message = "Some error!" });
                                }
                            }

                            await next(context);
                        });

                var app = builder.Build();

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
                });
                app.UseWebSockets();
                app.UseRouting();
                app.MapGraphQL();

                app.Start();

                return app;
            }
            catch
            {
                // we ignore any errors here and try the next port
            }
        }

        throw new InvalidOperationException("Not port found");
    }
}
