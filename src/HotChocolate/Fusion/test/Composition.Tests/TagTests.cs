using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;

namespace HotChocolate.Fusion.Composition;

public class TagTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Do_Not_Expose_Tags_On_Public_Schema()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionWithTagSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
            },
            FusionFeatureCollection.Empty);

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Expose_Tags_On_Public_Schema()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionWithTagSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.TagDirective(makeTagsPublic: true)));

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Exclude_Subgraphs_With_Review_Tag()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionWithTagSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionWithTagSdl),
            },
            new FusionFeatureCollection(FusionFeatures.TagDirective(
                makeTagsPublic: true,
                exclude: new[] {"review", })));

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Exclude_Type_System_Members_With_Internal_Tag()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionWithTagSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionWithTagSdl),
            },
            new FusionFeatureCollection(FusionFeatures.TagDirective(
                makeTagsPublic: true,
                exclude: new[] {"internal", })));

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Exclude_Type_System_Members_With_Internal_Tag_Which_Is_Private()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionWithTagSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionWithTagSdl),
            },
            new FusionFeatureCollection(FusionFeatures.TagDirective(
                makeTagsPublic: false,
                exclude: new[] {"internal", })));

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
}
