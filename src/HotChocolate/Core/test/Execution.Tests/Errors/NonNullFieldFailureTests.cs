using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class NonNullFieldFailureTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReportEveryError_When_SeveralNonNullFieldsOfNullableObjectFail()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                type Query {
                    item: Item
                }

                type Item {
                    a: String!
                    b: String!
                    c: String!
                }
                """)
            .AddResolver("Query", "item", _ => new("a"))
            .AddResolver("Item", "a", c => ResolveField(c, "a"))
            .AddResolver("Item", "b", c => ResolveField(c, "b"))
            .AddResolver("Item", "c", c => ResolveField(c, "c"))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ item { a b c } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The field b failed.",
                  "path": [
                    "item",
                    "b"
                  ]
                },
                {
                  "message": "The field c failed.",
                  "path": [
                    "item",
                    "c"
                  ]
                }
              ],
              "data": {
                "item": null
              }
            }
            """);
    }

    private static ValueTask<object?> ResolveField(IResolverContext context, string fieldName)
    {
        if (context.Parent<string>() == fieldName)
        {
            return new(fieldName);
        }

        throw new GraphQLException($"The field {fieldName} failed.");
    }
}
