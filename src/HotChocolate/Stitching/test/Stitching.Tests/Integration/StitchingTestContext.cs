using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Stitching.Integration;

public class StitchingTestContext
{
    public TestServerFactory ServerFactory { get; } = new();

    public string CustomerSchema { get; } = "customer";

    public string ContractSchema { get; } = "contract";

    public TestServer CreateCustomerService() =>
        ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.EnableTag = false)
                .AddCustomerSchema(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public TestServer CreateContractService() =>
        ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.EnableTag = false)
                .AddContractSchema(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public IHttpClientFactory CreateDefaultRemoteSchemas()
    {
        var connections = new Dictionary<string, HttpClient>
        {
            { CustomerSchema, CreateCustomerService().CreateClient() },
            { ContractSchema, CreateContractService().CreateClient() }
        };

        return CreateRemoteSchemas(connections);
    }

    public static IHttpClientFactory CreateRemoteSchemas(
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
}
