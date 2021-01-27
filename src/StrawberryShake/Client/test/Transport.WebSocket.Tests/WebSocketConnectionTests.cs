using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
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
using StrawberryShake.Transport.Subscriptions;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class HttpConnectionTests : ServerTestBase
    {
        public HttpConnectionTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Simple_Request()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(
                x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                out int port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"))
                .AddProtocol(new SubscriptionTransportWsProtocol());
            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            ISocketClientPool connectionPool =
                services.GetRequiredService<ISocketClientPool>();

            ISocketClient socketClient = await connectionPool.RentAsync("Foo");

            var document =
                new MockDocument("subscription Test { onTest }");
            var request = new OperationRequest("Test", document);

            // act
            var results = new List<JsonDocument>();
            await using var manager = new SocketOperationManager(socketClient.GetProtocol());
            var connection = new StrawberryShake.Http.Subscriptions.WebSocketConnection(manager);
            await foreach (var response in connection.ExecuteAsync(request))
            {
                if (response.Body is not null)
                {
                    results.Add(response.Body);
                }

                if (results.Count == 10)
                {
                    break;
                }
            }

            await connectionPool.ReturnAsync(socketClient);

            // assert
            results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
        }

        [ExtendObjectType("Subscription")]
        public class StringSubscriptionExtensions
        {
            [SubscribeAndResolve]
            public async IAsyncEnumerable<string> OnTest()
            {
                for (var i = 0; i < 10; i++)
                {
                    await Task.Delay(1);
                    yield return $"num{i}";
                }
            }
        }

        private class MockDocument : IDocument
        {
            private readonly byte[] _query;

            public MockDocument(string query)
            {
                _query = Encoding.UTF8.GetBytes(query);
            }

            public OperationKind Kind => OperationKind.Query;

            public ReadOnlySpan<byte> Body => _query;
        }
    }

    public static class TestServerHelper
    {
        public static IWebHost CreateServer(Action<IRequestExecutorBuilder> configure, out int port)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection();
            var config = configBuilder.Build();
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

            port = host.GetPort();
            return host;
        }
    }
}
