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
                    return n.Equals("contracts")
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
            executors["contracts"] = schema_contracts
                .MakeExecutable(b => b.UseStitchingPipeline("contracts"));
            executors["customers"] = schema_customers
                .MakeExecutable(b => b.UseStitchingPipeline("customers"));

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
                FileResource.Open("StitchingQuery.graphql"));

            result.Snapshot();
        }
    }
}
