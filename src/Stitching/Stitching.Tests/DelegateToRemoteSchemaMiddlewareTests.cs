using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
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
using HotChocolate.Resolvers;

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
        public Task ExecuteStitchingQueryWithInlineFragment()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithInlineFragment.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact]
        public Task ExecuteStitchingQueryWithFragmentDefinition()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithFragmentDefs.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact]
        public Task ExecuteStitchingQueryWithVariables()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithVariables.graphql"))
            {
                VariableValues = new Dictionary<string, object>
                {
                    {"customerId", "Q3VzdG9tZXIteDE="}
                }
            };

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact]
        public Task ExecuteStitchingQueryWithUnion()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithUnion.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact]
        public Task ExecuteStitchingQueryWithArguments()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithArguments.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact(Skip = "Not yet supported!")]
        public Task ExecuteStitchingQueryDeepArrayPath()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryDeepArrayPath.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact]
        public Task ExecuteStitchingQueryDeepObjectPath()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryDeepObjectPath.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        [Fact]
        public Task ExecuteStitchingQueryDeepScalarPath()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryDeepScalarPath.graphql"));

            // act and assert
            return ExecuteStitchedQuery(request);
        }

        private async Task ExecuteStitchedQuery(
            QueryRequest request,
            [CallerMemberName]string snapshotName = null)
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

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(httpClientFactory.Object);

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("contract")
                .SetSchema(FileResource.Open("Contract.graphql"))
                .AddScalarType<DateTimeType>());

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("customer")
                .SetSchema(FileResource.Open("Customer.graphql")));

            serviceCollection.AddStitchedSchema(
                FileResource.Open("Stitching.graphql"),
                c => c.RegisterType<DateTimeType>());

            IServiceProvider services =
                request.Services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.Snapshot(snapshotName);
        }

        [Fact]
        public async Task ExecuteStitchedQueryWithComputedField()
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

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(httpClientFactory.Object);

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("contract")
                .SetSchema(FileResource.Open("Contract.graphql"))
                .AddScalarType<DateTimeType>());

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("customer")
                .SetSchema(FileResource.Open("Customer.graphql")));

            serviceCollection.AddStitchedSchema(
                FileResource.Open("StitchingComputed.graphql"),
                c =>
                {
                    c.Map(new FieldReference("Customer", "foo"),
                        next => context =>
                        {
                            var obj = context.Parent<OrderedDictionary>();
                            context.Result = obj["name"] + "_" + obj["id"];
                            return Task.CompletedTask;
                        });
                    c.RegisterType<DateTimeType>();
                });

            var request = new QueryRequest(
                FileResource.Open("StitchingQueryComputedField.graphql"));

            IServiceProvider services =
                request.Services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.Snapshot();
        }
    }
}
