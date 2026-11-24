using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferDisabledOccurrenceTests
{
    [Fact]
    public async Task Execute_Should_DeliverFieldWithItsParent_When_OneOccurrenceIsInADisabledDefer()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        await using var result = await ExecuteAsync(
            executor,
            "query($d: Boolean!) { a { name ... @defer { x } ... @defer(if: $d) { x } } }",
            """{ "d": false }""");
        await using var plain = await ExecuteAsync(
            executor,
            "{ a { name x } }",
            "{}");

        // assert
        Snapshot
            .Create()
            .Add(result, "Incremental")
            .Add(plain, "Plain")
            .MatchMarkdown();
    }

    [Fact]
    public async Task Execute_Should_DeliverFieldWithItsDeferredParent_When_OneOccurrenceIsInADisabledDefer()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        await using var result = await ExecuteAsync(
            executor,
            "query($d: Boolean!) { ... @defer { a { name ... @defer { x } ... @defer(if: $d) { x } } } }",
            """{ "d": false }""");
        await using var plain = await ExecuteAsync(
            executor,
            "{ a { name x } }",
            "{}");

        // assert
        Snapshot
            .Create()
            .Add(result, "Incremental")
            .Add(plain, "Plain")
            .MatchMarkdown();
    }

    private static Task<IExecutionResult> ExecuteAsync(IRequestExecutor executor, string query, string variables)
        => executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(variables)
                .Build(),
            TestContext.Current.CancellationToken);

    private static ValueTask<IRequestExecutor> CreateExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(options => options.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public sealed class Query
    {
        public A A => new();
    }

    public sealed class A
    {
        public string Name => "name";

        public string X => "x";
    }
}
