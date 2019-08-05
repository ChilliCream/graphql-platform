using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Execution;
using HotChocolate.AspNetCore.Tests.Utilities;
using Snapshooter;

namespace HotChocolate.Stitching
{
    public class ScalarTests
        : StitchingTestBase
    {
        public ScalarTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [InlineData("date_field")]
        [InlineData("date_time_field")]
        [InlineData("string_field")]
        [InlineData("id_field")]
        [InlineData("byte_field")]
        [InlineData("int_field")]
        [InlineData("long_field")]
        [InlineData("float_field")]
        [InlineData("decimal_field")]
        [Theory]
        public async Task Scalar_Serializes_And_Deserializes_Correctly(
        string fieldName)
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer"));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery($@"
                            query a($contractId: ID!) {{
                                contract(contractId: $contractId) {{
                                    ... on LifeInsuranceContract {{
                                        {fieldName}
                                    }}
                                }}
                            }}")
                        .SetVariableValue(
                            "contractId",
                            "TGlmZUluc3VyYW5jZUNvbnRyYWN0LXgx")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension(fieldName));
        }
    }
}
