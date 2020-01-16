using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Client
{
    public class StitchingContextTests : StitchingTestBase
    {
        public StitchingContextTests(
           TestServerFactory testServerFactory)
           : base(testServerFactory)
        {
        }

        [Fact]
        public async Task CanExecuteRemoteQueryWithVaraibles()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CreateRemoteSchemas());
            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("contract")
                .AddSchemaFromHttp("customer"));

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            IStitchingContext stitchingContext = services.GetRequiredService<IStitchingContext>();
            IRemoteQueryClient customerQueryClient = stitchingContext.GetRemoteQueryClient("customer");

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query ($id: ID!) { customer(id: $id) { name } }")
                .SetVariableValue("id", "1")
                .Create();

            Task<IExecutionResult> executeTask = customerQueryClient.ExecuteAsync(request);
            await customerQueryClient.DispatchAsync(CancellationToken.None);

            IExecutionResult result = await executeTask;

            result.MatchSnapshot();
        }
    }
}
