#nullable enable

using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public sealed class OptInFeatureStabilityDirectiveTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("123abc")]
    [InlineData("!abc")]
    [InlineData("abc!")]
    [InlineData("a b c")]
    public void OptInFeatureStabilityDirective_InvalidFeatureName_ThrowsArgumentException(
        string? feature)
    {
        // arrange & act
        void Action() => _ = new OptInFeatureStabilityDirective(feature!, "stability");

        // assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("123abc")]
    [InlineData("!abc")]
    [InlineData("abc!")]
    [InlineData("a b c")]
    public void OptInFeatureStabilityDirective_InvalidStability_ThrowsArgumentException(
        string? stability)
    {
        // arrange & act
        void Action() => _ = new OptInFeatureStabilityDirective("feature", stability!);

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
                .SetSchema(s => s
                    .OptInFeatureStability("feature1", "stability1")
                    .OptInFeatureStability("feature2", "stability2"))
                .AddQueryType(d => d.Field("field").Type<IntType>())
                .UseField(_ => _ => default)
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
                    schema
                        @optInFeatureStability(feature: "feature1", stability: "stability1")
                        @optInFeatureStability(feature: "feature2", stability: "stability2") {
                        query: Query
                    }

                    type Query {
                        field: Int
                    }
                    """)
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }
}
