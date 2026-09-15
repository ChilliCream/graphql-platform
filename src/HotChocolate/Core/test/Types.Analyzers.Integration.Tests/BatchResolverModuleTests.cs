using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class BatchResolverModuleTests
{
    [Fact]
    public async Task BatchResolver_Should_Dispatch_When_TypeIsRegisteredThroughGeneratedModule()
    {
        // arrange, AddIntegrationTestTypesCore (the [Module] source-generated registration) must
        // wire BookBatchType.GetBatchGreeting's [BatchResolver] method into a BatchFieldDelegate
        // by discovering it from the assembly module, the same way every other BatchResolver
        // family in this project wires its resolver through an explicit per-type Initialize call.
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ product { ... on Book { batchGreeting } } }")
                .SetGlobalState("batchCurrentUser", new BatchCurrentUser("ada"))
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": {
                  "batchGreeting": "ada:GraphQL in Action"
                }
              }
            }
            """);
    }
}
