using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class QueryInstrumentationTests
{
    [Fact]
    public async Task Track_events_of_a_simple_query_detailed()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            var services = new ServiceCollection();
            services.AddGraphQLGateway()
                .AddInMemoryConfiguration(null)
                .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All);

            var provider = services.BuildServiceProvider().GetRequiredService<IRequestExecutorProvider>();
            var executor = await provider.GetExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }
}
