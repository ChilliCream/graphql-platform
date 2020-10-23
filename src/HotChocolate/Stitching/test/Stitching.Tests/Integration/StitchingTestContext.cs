using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using Moq;

namespace HotChocolate.Stitching.Integration
{
    public class StitchingTestContext
    {
        public TestServerFactory ServerFactory { get; } = new TestServerFactory();

        public NameString CustomerSchema { get; } = "customer";

        public NameString ContractSchema { get; } = "contract";

        public TestServer CreateCustomerService() =>
            ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddCustomerSchema(),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

        public TestServer CreateContractService() =>
            ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
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
}
