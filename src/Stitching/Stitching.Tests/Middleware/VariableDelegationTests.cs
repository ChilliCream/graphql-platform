using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using HotChocolate.AspNetCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Moq;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class VariableDelegationTests
        : StitchingTestBase
    {
        public VariableDelegationTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task ListVariableIsCorrectlyPassed()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            var connections = new Dictionary<string, HttpClient>();
            serviceCollection.AddSingleton(CreateRemoteSchemas(connections));

            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer"));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery("query foo ($ids: [ID!]!) " +
                        "{ customers(ids: $ids) { id } }")
                    .SetServices(scope.ServiceProvider)
                    .SetVariableValue("ids", new List<object>
                    {
                        "Q3VzdG9tZXIteDE=",
                        "Q3VzdG9tZXIteDE="
                    })
                    .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension("result"));
            executor.Schema.ToString().MatchSnapshot(
                new SnapshotNameExtension("schema"));
        }

        [Fact]
        public async Task ScopedListVariableIsCorrectlyPassed()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            var connections = new Dictionary<string, HttpClient>();
            serviceCollection.AddSingleton(CreateRemoteSchemas(connections));

            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer")
                .AddExtensionsFromString(
                @"
                    extend type Query {
                        allCustomers: [Customer!]
                            @delegate(
                                path: ""customers(ids: $contextData:ids)""
                                schema: ""customer"")
                    }
                "));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery("query foo { allCustomers { id } }")
                    .SetServices(scope.ServiceProvider)
                    .SetProperty("ids", new List<object>
                    {
                        "Q3VzdG9tZXIteDE=",
                        "Q3VzdG9tZXIteDE="
                    })
                    .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension("result"));
            executor.Schema.ToString().MatchSnapshot(
                new SnapshotNameExtension("schema"));
        }


    }
}
