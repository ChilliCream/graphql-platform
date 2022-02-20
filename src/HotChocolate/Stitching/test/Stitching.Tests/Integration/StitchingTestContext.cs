using System;
using System.Collections.Generic;
using System.Net.Http;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Transport.WebSockets.Protocols;
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
        client.ConfigureRequest = r => r.Headers.Add("Sec-WebSocket-Protocol", "graphql-ws");

        var connections = new Dictionary<string, ISocketClient>
        {
            {
                CustomerSchema,
                new TestWebSocketClient(
                    CustomerSchema,
                    new ISocketProtocolFactory[] { new GraphQLWebSocketProtocolFactory() },
                    (uri, ct) => client.ConnectAsync(uri, ct))
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

    public static ISocketClientFactory CreateSocketClientFactory(
        Dictionary<string, ISocketClient> connections)
    {
        var httpClientFactory = new Mock<ISocketClientFactory>();
        httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
            .Returns(new Func<string, ISocketClient>(n =>
            {
                if (connections.ContainsKey(n))
                {
                    return connections[n];
                }

                throw new Exception();
            }));

        return httpClientFactory.Object;
    }
}
