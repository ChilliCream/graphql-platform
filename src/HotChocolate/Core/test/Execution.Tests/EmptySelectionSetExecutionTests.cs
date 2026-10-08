using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public sealed class EmptySelectionSetExecutionTests
{
    public static TheoryData<string, string, Dictionary<string, object?>?> Shapes =>
        new()
        {
            { "EmptyQuery", "{ }", null },
            { "EmptyNamedQuery", "query Q { }", null },
            { "EmptyMutation", "mutation { }", null },
            { "EmptySubscription", "subscription { }", null },
            { "EmptyCompositeField", "{ hero(episode: NEW_HOPE) { } }", null },
            { "EmptyListItems", "{ heroes { } }", null },
            { "EmptyInlineFragment", "{ hero(episode: NEW_HOPE) { ... on Droid { } } }", null },
            { "EmptyInlineFragmentUntyped", "{ hero(episode: NEW_HOPE) { ... { } } }", null },
            {
                "EmptyNamedFragment",
                "{ hero(episode: NEW_HOPE) { ...abc } } fragment abc on Droid { }",
                null
            },
            {
                "EmptyNamedFragmentBesideIncludedField_IncludeTrue",
                "query foo($v: Boolean!) { hero(episode: NEW_HOPE) { name @include(if: $v) ...abc } } fragment abc on Droid { }",
                new Dictionary<string, object?> { ["v"] = true }
            },
            {
                "EmptyNamedFragmentBesideIncludedField_IncludeFalse",
                "query foo($v: Boolean!) { hero(episode: NEW_HOPE) { name @include(if: $v) ...abc } } fragment abc on Droid { }",
                new Dictionary<string, object?> { ["v"] = false }
            },
            {
                "EmptyInlineFragmentBesideIncludedField_IncludeTrue",
                "query foo($v: Boolean!) { hero(episode: NEW_HOPE) { name @include(if: $v) ... on Droid { } } }",
                new Dictionary<string, object?> { ["v"] = true }
            },
            {
                "EmptyInlineFragmentBesideIncludedField_IncludeFalse",
                "query foo($v: Boolean!) { hero(episode: NEW_HOPE) { name @include(if: $v) ... on Droid { } } }",
                new Dictionary<string, object?> { ["v"] = false }
            }
        };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task Execute_Should_MatchSnapshot_When_EmptySelectionSet(
        string name,
        string document,
        Dictionary<string, object?>? variables)
    {
        // arrange
        var enabledExecutor = await CreateRequestExecutorAsync(enableEmptySelectionSets: true);
        var disabledExecutor = await CreateRequestExecutorAsync(enableEmptySelectionSets: false);

        // act
        var enabledResult = await enabledExecutor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(document)
                .SetVariableValues(variables)
                .Build(),
            TestContext.Current.CancellationToken);
        var disabledResult = await disabledExecutor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(document)
                .SetVariableValues(variables)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        enabledResult.ToJson().MatchSnapshot(postFix: $"{name}_Enabled");
        disabledResult.ToJson().MatchSnapshot(postFix: $"{name}_Disabled");
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
            .ModifyCostOptions(o => o.DefaultListSize = 1)
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
