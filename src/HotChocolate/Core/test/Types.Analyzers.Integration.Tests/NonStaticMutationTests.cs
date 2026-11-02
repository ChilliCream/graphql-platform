using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class NonStaticMutationTests
{
    [Fact]
    public async Task Mutation_Should_InvokeInstanceResolver_When_MutationConventionsAndDescriptorAttributeAreApplied()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            mutation {
              renameShape(input: { name: "circle" }) {
                result
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "renameShape": {
                  "result": "non-static-mutation:circle"
                }
              }
            }
            """);
    }
}
