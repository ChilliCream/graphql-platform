using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public sealed class EmptySelectionSetExecutionTests
{
    [Fact]
    public async Task Execute_Should_ReturnEmptyData_When_EmptySelectionSetsAreEnabled()
    {
        // arrange
        var executor = await CreateRequestExecutorAsync(enableEmptySelectionSets: true);

        // act
        var results = new[]
        {
            await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken),
            await executor.ExecuteAsync("query Q { }", TestContext.Current.CancellationToken),
            await executor.ExecuteAsync("mutation { }", TestContext.Current.CancellationToken),
            await executor.ExecuteAsync("{ hero(episode: NEW_HOPE) { } }", TestContext.Current.CancellationToken),
            await executor.ExecuteAsync("{ heroes { } }", TestContext.Current.CancellationToken),
            await executor.ExecuteAsync(
                "{ hero(episode: NEW_HOPE) { ... on Droid { } } }",
                TestContext.Current.CancellationToken)
        };

        // assert
        results.Select(t => t.ToJson()).MatchInlineSnapshots(
        [
            """
            {
              "data": {}
            }
            """,
            """
            {
              "data": {}
            }
            """,
            """
            {
              "data": {}
            }
            """,
            """
            {
              "data": {
                "hero": {}
              }
            }
            """,
            """
            {
              "data": {
                "heroes": [
                  {},
                  {}
                ]
              }
            }
            """,
            """
            {
              "data": {
                "hero": {}
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Execute_Should_ReturnValidationErrors_When_SubscriptionSelectionSetIsEmpty()
    {
        // arrange
        var executor = await CreateRequestExecutorAsync(enableEmptySelectionSets: true);

        // act
        var result = await executor.ExecuteAsync("subscription { }", TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Operation `Unnamed` has an empty selection set. Root types without selections are disallowed.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 1
                    }
                  ],
                  "extensions": {
                    "operation": "Unnamed",
                    "type": "Subscription",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                },
                {
                  "message": "Subscription operations must have exactly one root field.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 1
                    }
                  ],
                  "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Single-Root-Field"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ReturnValidationError_When_EmptySelectionSetsAreDisabled()
    {
        // arrange
        var executor = await CreateRequestExecutorAsync(enableEmptySelectionSets: false);

        // act
        var result = await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Operation `Unnamed` has an empty selection set. Root types without selections are disallowed.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 1
                    }
                  ],
                  "extensions": {
                    "operation": "Unnamed",
                    "type": "Query",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                }
              ]
            }
            """);
    }

    private static ValueTask<IRequestExecutor> CreateRequestExecutorAsync(bool enableEmptySelectionSets)
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType(
                d => d.Field("onDroid")
                    .Type<StringType>()
                    .Resolve(_ => new ValueTask<object?>("R2-D2")))
            .ModifyOptions(o => o.EnableEmptySelectionSets = enableEmptySelectionSets)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public sealed class Query
    {
        public Droid Hero(Episode episode) => new();

        public IReadOnlyList<Droid> Heroes => [new(), new()];
    }

    public sealed class Mutation
    {
        public Droid UpdateHero() => new();
    }

    public sealed class Droid
    {
        public string Name => "R2-D2";
    }

    public enum Episode
    {
        NewHope
    }
}
