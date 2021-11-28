using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.PrefixedFieldsWithUnderscore
{
    public class PrefixedFieldsWithUnderscoreTest : ServerTestBase
    {
        public PrefixedFieldsWithUnderscoreTest(TestServerFactory serverFactory) : base(
            serverFactory)
        {
        }

        [Fact]
        public async Task Execute_PrefixedFieldsWithUnderscore_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = CreateServer(out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                PrefixedFieldsWithUnderscoreClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                PrefixedFieldsWithUnderscoreClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddPrefixedFieldsWithUnderscoreClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            PrefixedFieldsWithUnderscoreClient client =
                services.GetRequiredService<PrefixedFieldsWithUnderscoreClient>();

            // act
            IOperationResult<IGetExampleResult> result = await client.GetExample.ExecuteAsync(ct);

            // assert
            result.Data.MatchSnapshot();
        }

        public static IWebHost CreateServer(out int port)
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
                            services.AddRouting()
                                .AddGraphQLServer()
                                .AddQueryType(x =>
                                    x.Name("Query").Field("_example").Resolve(() => "Foo"))
                        )
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
