using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

// Schema-first SDL exercises the directive's literal parser; descriptor-based configuration bypasses it.
public sealed class ListSizeDirectiveTypeTests
{
    [Fact]
    public async Task ParseLiteral_Should_Default_To_True_When_RequireOneSlicingArgument_Omitted()
    {
        // arrange & act
        var schema = await CreateSchemaAsync(
            """examples(limit: Int): [String!]! @listSize(slicingArguments: ["limit"])""");

        // assert
        var directive = GetListSizeDirective(schema);
        Assert.True(directive.RequireOneSlicingArgument);
    }

    [Fact]
    public async Task ParseLiteral_Should_Honor_Explicit_RequireOneSlicingArgument_False()
    {
        // arrange & act
        var schema = await CreateSchemaAsync(
            """
            examples(limit: Int): [String!]!
                @listSize(slicingArguments: ["limit"], requireOneSlicingArgument: false)
            """);

        // assert
        var directive = GetListSizeDirective(schema);
        Assert.False(directive.RequireOneSlicingArgument);
    }

    [Fact]
    public async Task ParseLiteral_Should_Honor_Explicit_RequireOneSlicingArgument_True()
    {
        // arrange & act
        var schema = await CreateSchemaAsync(
            """
            examples(limit: Int): [String!]!
                @listSize(slicingArguments: ["limit"], requireOneSlicingArgument: true)
            """);

        // assert
        var directive = GetListSizeDirective(schema);
        Assert.True(directive.RequireOneSlicingArgument);
    }

    [Fact]
    public async Task ParseLiteral_Should_Stay_False_When_No_SlicingArguments_Are_Declared()
    {
        // arrange & act
        var schema = await CreateSchemaAsync(
            "examples(limit: Int): [String!]! @listSize(assumedSize: 10)");

        // assert
        var directive = GetListSizeDirective(schema);
        Assert.False(directive.RequireOneSlicingArgument);
    }

    private static async Task<Schema> CreateSchemaAsync(string exampleField)
        => await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(
                $$"""
                type Query {
                    {{exampleField}}
                }
                """)
            .AddResolver("Query", "examples", _ => new List<string>())
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static ListSizeDirective GetListSizeDirective(Schema schema)
        => schema.QueryType.Fields["examples"]
            .Directives
            .Single(d => d.Name == "listSize")
            .ToValue<ListSizeDirective>();
}
