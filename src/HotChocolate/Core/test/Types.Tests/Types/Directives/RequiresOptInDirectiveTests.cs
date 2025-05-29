#nullable enable

using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public sealed class RequiresOptInDirectiveTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("123abc")]
    [InlineData("!abc")]
    [InlineData("abc!")]
    [InlineData("a b c")]
    public void RequiresOptInDirective_InvalidFeatureName_ThrowsArgumentException(string? feature)
    {
        // arrange & act
        void Action() => _ = new RequiresOptInDirective(feature!);

        // assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Fact]
    public async Task BuildSchemaAsync_CodeFirst_MatchesSnapshot()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o => o.EnableOptInFeatures = true)
                .AddQueryType(d => d
                    .Field("field")
                    .Type<IntType>()
                    .Argument(
                        "argument",
                        a => a
                            .Type<IntType>()
                            .RequiresOptIn("objectFieldArgFeature1")
                            .RequiresOptIn("objectFieldArgFeature2"))
                    .Resolve(() => 1)
                    .RequiresOptIn("objectFieldFeature1")
                    .RequiresOptIn("objectFieldFeature2"))
                .AddInputObjectType(d => d
                    .Name("Input")
                    .Field("field")
                    .Type<IntType>()
                    .RequiresOptIn("inputFieldFeature1")
                    .RequiresOptIn("inputFieldFeature2"))
                .AddEnumType(d => d
                    .Name("Enum")
                    .Value("VALUE")
                    .RequiresOptIn("enumValueFeature1")
                    .RequiresOptIn("enumValueFeature2"))
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task BuildSchemaAsync_ImplementationFirst_MatchesSnapshot()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o => o.EnableOptInFeatures = true)
                .AddQueryType<Query>()
                .AddInputObjectType<Input>()
                .AddType<Enum>()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task BuildSchemaAsync_SchemaFirst_MatchesSnapshot()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o => o.EnableOptInFeatures = true)
                .AddDocumentFromString(
                    """
                    type Query {
                        field(
                            argument: Int
                                @requiresOptIn(feature: "objectFieldArgFeature1")
                                @requiresOptIn(feature: "objectFieldArgFeature2")): Int
                                    @requiresOptIn(feature: "objectFieldFeature1")
                                    @requiresOptIn(feature: "objectFieldFeature2")
                    }

                    input Input {
                        field: Int
                            @requiresOptIn(feature: "inputFieldFeature1")
                            @requiresOptIn(feature: "inputFieldFeature2")
                    }

                    enum Enum {
                        VALUE
                            @requiresOptIn(feature: "enumValueFeature1")
                            @requiresOptIn(feature: "enumValueFeature2")
                    }
                    """)
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    public sealed class Query
    {
        [RequiresOptIn("objectFieldFeature1")]
        [RequiresOptIn("objectFieldFeature2")]
        public int? GetField(
            [RequiresOptIn("objectFieldArgFeature1")]
            [RequiresOptIn("objectFieldArgFeature2")] int? argument)
            => argument;
    }

    public sealed class Input
    {
        [RequiresOptIn("inputFieldFeature1")]
        [RequiresOptIn("inputFieldFeature2")]
        public int? Field { get; set; }
    }

    public enum Enum
    {
        [RequiresOptIn("enumValueFeature1")]
        [RequiresOptIn("enumValueFeature2")]
        Value
    }
}
