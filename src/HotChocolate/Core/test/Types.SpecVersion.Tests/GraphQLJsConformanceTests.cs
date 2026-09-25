using HotChocolate.Serialization;
using HotChocolate.StarWars;

namespace HotChocolate.Types.SpecVersion;

public sealed class GraphQLJsConformanceTests(GraphQLJsFixture fixture) : IClassFixture<GraphQLJsFixture>
{
    [Fact]
    public async Task DowngradedSchemas_Should_Validate_When_UsingPinnedGraphQLJsEditions()
    {
        // graphql@16.8.2 accepts argument deprecations, so rewriter snapshots cover that rule.
        // arrange
        fixture.SkipWhenUnavailable();
        var fixtureSchema = CreateFixtureSchema();
        var starWarsSchema = SchemaBuilder.New().AddStarWarsTypes().Create();
        var results = new List<ValidationCase>();

        // act
        foreach (var (version, alias) in s_editions)
        {
            foreach (var (name, schema) in Schemas(fixtureSchema, starWarsSchema))
            {
                foreach (var semanticNonNull in new[] { false, true })
                {
                    var sdl = SchemaFormatter.FormatAsString(
                        schema,
                        new SchemaFormatterOptions
                        {
                            SpecVersion = version,
                            RewriteToSemanticNonNull = semanticNonNull
                        });
                    var result = await fixture.ValidateAsync(alias, sdl);
                    results.Add(new ValidationCase(alias, name, semanticNonNull, result));
                }
            }
        }

        // assert
        results.MatchMarkdownSnapshot();
        Assert.All(results, result => Assert.True(result.Result.IsSuccess));
    }

    [Fact]
    public async Task NativeFixtureSchema_Should_BeRejected_When_UsingPinnedGraphQLJsEditions()
    {
        // arrange
        fixture.SkipWhenUnavailable();
        var nativeSchema = SchemaFormatter.FormatAsString(CreateFixtureSchema());
        var results = new List<ValidationCase>();

        // act
        foreach (var (_, alias) in s_editions)
        {
            var result = await fixture.ValidateAsync(alias, nativeSchema);
            results.Add(new ValidationCase(alias, "nativeFixture", false, result));
        }

        // assert
        new
        {
            HasDirectiveDefinition = nativeSchema.Contains("DIRECTIVE_DEFINITION", StringComparison.Ordinal),
            HasTagDirective = nativeSchema.Contains("directive @tag", StringComparison.Ordinal),
            HasRequiresOptInDirective = nativeSchema.Contains(
                "directive @requiresOptIn",
                StringComparison.Ordinal),
            HasDeprecatedDirective = nativeSchema.Contains(
                "directive @deprecatedDirective",
                StringComparison.Ordinal),
            HasDirectiveDefinitionOnlyDirective = nativeSchema.Contains(
                "directive @directiveDefinitionOnly",
                StringComparison.Ordinal),
            Results = results
        }.MatchMarkdownSnapshot();
        Assert.All(results, result => Assert.False(result.Result.IsSuccess));
        Assert.All(
            results,
            result => Assert.Contains(
                "Syntax Error: Unexpected Name \"DIRECTIVE_DEFINITION\".",
                result.Result.StandardError,
                StringComparison.Ordinal));
    }

    private static readonly (GraphQLSpecVersion Version, string Alias)[] s_editions =
    [
        (GraphQLSpecVersion.October2021, "graphql-october-2021"),
        (GraphQLSpecVersion.September2025, "graphql-september-2025")
    ];

    private sealed record ValidationCase(
        string Edition,
        string Schema,
        bool SemanticNonNull,
        ValidationResult Result);

    private static IEnumerable<(string Name, ISchemaDefinition Schema)> Schemas(
        ISchemaDefinition fixtureSchema,
        ISchemaDefinition starWarsSchema)
    {
        yield return ("fixture", fixtureSchema);
        yield return ("starWars", starWarsSchema);
    }

    private static ISchemaDefinition CreateFixtureSchema()
    {
        var custom = new DirectiveType(d => d
            .Name("custom")
            .Location(DirectiveLocation.Object)
            .Location(DirectiveLocation.DirectiveDefinition));
        var directiveDefinitionOnly = new DirectiveType(d => d
            .Name("directiveDefinitionOnly")
            .Location(DirectiveLocation.DirectiveDefinition));
        var deprecatedDirective = new DirectiveType(d => d
            .Name("deprecatedDirective")
            .Location(DirectiveLocation.Object)
            .Deprecated("Use custom instead.")
            .Directive("directiveDefinitionOnly"));

        return SchemaBuilder.New()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddQueryType(d => d
                .Name("Query")
                .Tag("fixture")
                .Field("value")
                .Type<NonNullType<StringType>>()
                .Argument("deprecatedArgument", a => a.Type<StringType>().Deprecated("Use input."))
                .Argument("defaultDeprecatedArgument", a => a.Type<StringType>().Deprecated())
                .RequiresOptIn("fixture")
                .Resolve("value"))
            .AddInputObjectType(d =>
            {
                d.Name("Filter").OneOf();
                d.Field("deprecatedInput").Type<StringType>().Deprecated("Use currentInput.");
                d.Field("defaultDeprecatedInput").Type<StringType>().Deprecated();
            })
            .AddType(new UrlType("FixtureUrl"))
            .AddDirectiveType(custom)
            .AddDirectiveType(directiveDefinitionOnly)
            .AddDirectiveType(deprecatedDirective)
            .AddType(new ObjectType(d => d
                .Name("Tagged")
                .Directive("custom")
                .Directive("deprecatedDirective")
                .Field("value")
                .Type<NonNullType<StringType>>()
                .Resolve("value")))
            .Create();
    }
}
