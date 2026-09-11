using CookieCrumble;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Execution;

public class BatchResolverOperationCompilerTests
{
    [Theory]
    [InlineData("regular", false)]
    [InlineData("batch", false)]
    [InlineData("both", false)]
    [InlineData("regular", true)]
    [InlineData("batch", true)]
    public void OperationCompiler_Should_Reject_When_ExecutableDirectiveHasMiddlewareOnBatchSelection(
        string middleware,
        bool merged)
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDirectiveType(new DirectiveType(d =>
            {
                d.Name("mark").Location(DirectiveLocation.Field);
                if (middleware is "regular" or "both")
                {
                    d.Use((FieldDelegate next, Directive _) => next);
                }

                if (middleware is "batch" or "both")
                {
                    d.UseBatch((BatchFieldDelegate next, Directive _) => next);
                }
            }))
            .AddQueryType(d => d.Name("Query").Field("value").Type<StringType>()
                .ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                    contexts.Select(_ => ResolverResult.Ok("value")).ToArray())))
            .Create();
        var document = Utf8GraphQLParser.Parse(merged
            ? "{ value ... { value @mark } }"
            : "{ alias: value @mark }");

        // act
        var exception = Assert.Throws<GraphQLException>(() =>
            OperationCompiler.Compile("test", document, schema));

        // assert
        new Snapshot(postFix: merged ? "merged" : "alias")
            .Add(exception.Errors, "Compilation errors")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task OperationCompiler_Should_ExecuteBatch_When_DirectivesHaveNoMiddleware()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddDirectiveType(new DirectiveType(d => d.Name("mark").Location(DirectiveLocation.Field)))
            .AddQueryType(d => d.Field("value").Type<IntType>()
                .Argument("input", a => a.Type<NonNullType<IntType>>())
                .ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                    contexts.Select(c => ResolverResult.Ok(c.ArgumentValue<int>("input"))).ToArray())))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ a: value(input: 1) @mark @include(if: true) b: value(input: 2) @skip(if: false) hidden: value(input: 3) @skip(if: true) }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": 1,
                "b": 2
              }
            }
            """);
    }

    [Fact]
    public async Task OperationCompiler_Should_RunRegularMiddleware_When_ExecutableDirectiveHasBothKinds()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddDirectiveType(new DirectiveType(d => d.Name("mark").Location(DirectiveLocation.Field)
                .Use((next, _) => async context =>
                {
                    await next(context);
                    context.Result = $"regular({context.Result})";
                })
                .UseBatch((next, _) => async contexts =>
                {
                    await next(contexts);
                    foreach (var context in contexts)
                    {
                        context.Result = $"batch({context.Result})";
                    }
                })))
            .AddQueryType(d => d.Field("value").Resolve("value"))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ value @mark }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "value": "regular(value)"
              }
            }
            """);
    }
}
