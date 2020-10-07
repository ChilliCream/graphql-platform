using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Integration
{
    public class BaseTests : IClassFixture<StitchingTestContext>
    {
        public BaseTests(StitchingTestContext context)
        {
            Context = context;
        }

        protected StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_Schema()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task AutoMerge_Execute()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchema(Context.ContractSchema)
                    .AddRemoteSchema(Context.CustomerSchema)
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"{
                    allCustomers {
                        id
                        name
                    }
                }");

            // assert
            result.MatchSnapshot();
        }
    }
}
