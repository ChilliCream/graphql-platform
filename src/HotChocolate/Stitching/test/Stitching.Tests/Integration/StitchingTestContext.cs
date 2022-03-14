using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Stitching.Execution;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebSocketClient = Microsoft.AspNetCore.TestHost.WebSocketClient;

namespace HotChocolate.Stitching.Integration;

public class StitchingTestContext
{
    public TestServerFactory ServerFactory { get; } = new();

    public NameString CustomerSchema => "customer";

    public NameString ContractSchema => "contract";

    public TestServer CreateCustomerService() =>
        ServerFactory.Create(
            services => services
                .AddRouting()
                .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                .AddGraphQLServer()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .AddCustomerSchema(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public TestServer CreateContractService() =>
        ServerFactory.Create(
            services => services
                .AddRouting()
                .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                .AddGraphQLServer()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .AddContractSchema(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public IHttpClientFactory CreateDefaultHttpClientFactory()
    {
        var connections = new Dictionary<string, HttpClient>
            {
                { CustomerSchema, CreateCustomerService().CreateClient() },
                { ContractSchema, CreateContractService().CreateClient() }
            };

        return CreateHttpClientFactory(connections);
    }

    public ISocketClientFactory CreateDefaultWebSocketClientFactory()
    {
        WebSocketClient client = CreateCustomerService().CreateWebSocketClient();
        client.ConfigureRequest =
            r => r.Headers.Add("Sec-WebSocket-Protocol", "graphql-transport-ws");

        var connections = new Dictionary<string, Func<CancellationToken, ValueTask<WebSocket>>>
        {
            {
                CustomerSchema,
                async ct => await client.ConnectAsync(new Uri("ws://localhost:5000/graphql"), ct)
            },
        };

        return CreateSocketClientFactory(connections);
    }

    public static IHttpClientFactory CreateHttpClientFactory(
        Dictionary<string, HttpClient> connections)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
            .Returns(new Func<string, HttpClient>(n =>
            {
                if (connections.ContainsKey(n))
                {
                    return connections[n];
                }

                throw new Exception();
            }));

        return httpClientFactory.Object;
    }

    private static ISocketClientFactory CreateSocketClientFactory(
        Dictionary<string, Func<CancellationToken, ValueTask<WebSocket>>> connections)
    {
        var httpClientFactory = new Mock<ISocketClientFactory>();
        httpClientFactory
            .Setup(t => t.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new Func<string, CancellationToken, ValueTask<SocketClient>>(
                async (n, ct) =>
                {
                    if (connections.ContainsKey(n))
                    {
                        return await SocketClient.ConnectAsync(await connections[n](ct), ct);
                    }

                    throw new Exception($"{n} is not configured as a socket.");
                }));

        return httpClientFactory.Object;
    }
}
