using CookieCrumble;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;

namespace HotChocolate.Fusion.Composition;

public sealed class DemoIntegrationTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Accounts_And_Reviews()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
            });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
    
    [Fact]
    public async Task Accounts_And_Reviews_Infer_Patterns()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
            });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Products()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Products_With_Nodes()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews2_Products_With_Nodes()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Products_AutoCompose_With_Node()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(),
                demoProject.Reviews.ToConfiguration(),
                demoProject.Products.ToConfiguration(),
            });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
}
