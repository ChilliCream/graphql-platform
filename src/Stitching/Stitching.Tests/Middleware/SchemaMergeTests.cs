
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HotChocolate.Execution;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class SchemaMergeTests
        : StitchingTestBase
    {
        public SchemaMergeTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task IsDirectiveMergeRuleTriggered()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            bool triggered = false;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer")
                .AddDirectiveMergeRule(next => (c, d) =>
                {
                    triggered = true;
                    next(c, d);
                }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                // some query to trigger the merging of the schemas
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery($@"
                            query a($contractId: ID!) {{
                                contract(contractId: $contractId) {{
                                    ... on LifeInsuranceContract {{
                                        id
                                    }}
                                }}
                            }}")
                        .SetVariableValue(
                            "contractId",
                            "TGlmZUluc3VyYW5jZUNvbnRyYWN0CmQx")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Assert.True(triggered);
        }

        [Fact]
        public async Task IsTypeMergeRuleTriggered()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            bool triggered = false;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer")
                .AddTypeMergeRule(next => (c, t) =>
                {
                    triggered = true;
                    next(c, t);
                }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                // some query to trigger the merging of the schemas
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery($@"
                            query a($contractId: ID!) {{
                                contract(contractId: $contractId) {{
                                    ... on LifeInsuranceContract {{
                                        id
                                    }}
                                }}
                            }}")
                        .SetVariableValue(
                            "contractId",
                            "TGlmZUluc3VyYW5jZUNvbnRyYWN0CmQx")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Assert.True(triggered);
        }
    }
}
