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

        public async Task Bar()
        {
            IdSerializer s = new IdSerializer();
            string id = s.Serialize("Customer", "1");

            var x = "query fetch {\n  contracts(customerId: $fields_id) {\n    id\n    ... on LifeInsuranceContract {\n      premium\n      __typename\n    }\n    ... on SomeOtherContract {\n      expiryDate\n      __typename\n    }\n    __typename\n  }\n}";
            ISchema schema = ContractSchemaFactory.Create();
            var serviceCollection = new ServiceCollection();
            ContractSchemaFactory.ConfigureServices(serviceCollection);
            IQueryExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest(x)
                {
                    Services = serviceCollection.BuildServiceProvider(),
                    VariableValues = new Dictionary<string, object> { { "fields_id", "Q3VzdG9tZXIteDE=" }}
                });
            result.Snapshot();
        }

        [Fact]
        public async Task Foo()
        {
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

            ISchema schema_contracts = Schema.Create(
                FileResource.Open("Contract.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.Use(next => context => Task.CompletedTask);
                });

            ISchema schema_customers = Schema.Create(
                FileResource.Open("Customer.graphql"),
                c =>
                {
                    c.Use(next => context => Task.CompletedTask);
                });

            var executors = new Dictionary<string, IQueryExecutor>();
            executors["contract"] = schema_contracts
                .MakeExecutable(b => b.UseStitchingPipeline("contract"));
            executors["customer"] = schema_customers
                .MakeExecutable(b => b.UseStitchingPipeline("customer"));

            var services = new ServiceCollection();
            services.AddSingleton(httpClientFactory.Object);
            services.AddSingleton<IStitchingContext>(
                new StitchingContext(executors));

            ISchema schema = Schema.Create(
                FileResource.Open("Stitching.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.RegisterDirective<SchemaDirectiveType>();
                    c.Use<DelegateToRemoteSchemaMiddleware>();
                    c.Use<DictionaryResultMiddleware>();
                });

            IQueryExecutor executor = schema.MakeExecutable(b =>
                b.Use<CopyVariablesToResolverContext>().UseDefaultPipeline());

            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest(FileResource.Open("StitchingQuery.graphql"))
                {
                    Services = services.BuildServiceProvider()
                });

            result.Snapshot();
        }
    }
}
