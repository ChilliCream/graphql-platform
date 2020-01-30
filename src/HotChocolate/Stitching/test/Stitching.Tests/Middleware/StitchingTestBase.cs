using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit;
using HotChocolate.AspNetCore;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class StitchingTestBase
        : IClassFixture<TestServerFactory>
    {
        public StitchingTestBase(
            TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        protected TestServerFactory TestServerFactory { get; set; }

        protected IHttpClientFactory CreateRemoteSchemas()
        {
            return CreateRemoteSchemas(new Dictionary<string, HttpClient>());
        }

        protected virtual IHttpClientFactory CreateRemoteSchemas(
            Dictionary<string, HttpClient> connections)
        {
            TestServer server_contracts = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

            TestServer server_customers = TestServerFactory.Create(
                CustomerSchemaFactory.ConfigureServices,
                app => app.UseGraphQL());

            connections["contract"] = server_contracts.CreateClient();
            connections["customer"] = server_customers.CreateClient();

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
