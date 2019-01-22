using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddlewareTests
        : IClassFixture<TestServerFactory>
    {
        public DelegateToRemoteSchemaMiddlewareTests(
            TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        private TestServerFactory TestServerFactory { get; set; }

        [Fact]
        public async Task ExecuteStitchingQueryWithInterfaceFragment()
        {
            // arrange
            TestServer server_contracts = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureSchema,
                ContractSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            TestServer server_customers = TestServerFactory.Create(
                CustomerSchemaFactory.ConfigureSchema,
                CustomerSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
                .Returns(new Func<string, HttpClient>(n =>
                {
                    return n.Equals("contract")
                        ? server_contracts.CreateClient()
                        : server_customers.CreateClient();
                }));

            IStitchingContext stitchingContext = StitchingContextBuilder.New()
                .AddExecutor(b => b
                    .SetSchemaName("contract")
                    .SetSchema(FileResource.Open("Contract.graphql"))
                    .AddScalarType<DateTimeType>())
                .AddExecutor(b => b
                    .SetSchemaName("customer")
                    .SetSchema(FileResource.Open("Customer.graphql")))
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(httpClientFactory.Object);
            services.AddSingleton(stitchingContext);

            ISchema schema = Schema.Create(
                FileResource.Open("Stitching.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.UseSchemaStitching();
                });

            IQueryExecutor executor = schema.MakeExecutable(
                b => b.UseStitchingPipeline());

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest(FileResource.Open("StitchingQuery.graphql"))
                {
                    Services = services.BuildServiceProvider()
                });

            // assert
            result.Snapshot();
        }
    }
}
