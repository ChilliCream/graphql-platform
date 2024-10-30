using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Shared;
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
        [
            demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl)
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Infer_Patterns()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
        [
            demoProject.Accounts.ToConfiguration(),
            demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl)
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Products()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
        [
            demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
            demoProject.Products.ToConfiguration(ProductsExtensionSdl)
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Products_With_Nodes()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            [
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            ],
            new FusionFeatureCollection(FusionFeatures.NodeField));

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews2_Products_With_Nodes()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            [
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            ],
            new FusionFeatureCollection(FusionFeatures.NodeField));

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Accounts_And_Reviews_Products_AutoCompose_With_Node()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
        [
            demoProject.Accounts.ToConfiguration(),
            demoProject.Reviews.ToConfiguration(),
            demoProject.Products.ToConfiguration()
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Compose_With_SourceSchema_Lib()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
        [
            demoProject.Accounts.ToConfiguration(),
            demoProject.Reviews.ToConfiguration(),
            demoProject.Products.ToConfiguration(),
            demoProject.Shipping2.ToConfiguration()
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task User_Field_Is_Fully_Specified_Lookup()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
        [
            new SubgraphConfiguration(
                "Schema1",
                """
                schema {
                  query: Query
                }

                type Query {
                  user(id: Int! @is(field: "id")): User @lookup
                }

                type User {
                  id: Int!
                  name: String!
                  email: String!
                  password: String!
                }

                """,
                Array.Empty<string>(),
                [
                    new HttpClientConfiguration(
                        new Uri("http://localhost:5000/graphql"),
                        "Schema1")
                ],
                default)
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task User_Field__Lookup_Infers_Is_Directive()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
        [
            new SubgraphConfiguration(
                "Schema1",
                """
                schema {
                  query: Query
                }

                type Query {
                  user(id: Int!): User @lookup
                }

                type User {
                  id: Int!
                  name: String!
                  email: String!
                  password: String!
                }

                """,
                Array.Empty<string>(),
                [
                    new HttpClientConfiguration(
                        new Uri("http://localhost:5000/graphql"),
                        "Schema1")
                ],
                default)
        ]);

        fusionConfig.MatchSnapshot(extension: ".graphql");
    }
}
