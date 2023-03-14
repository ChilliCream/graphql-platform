using CookieCrumble;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public sealed class DemoIntegrationTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public DemoIntegrationTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

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
            FusionFeatureFlags.NodeField);

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }

    private const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: ID! @is(field: "id")): User!
          usersById(ids: [ID!]! @is(field: "id")): [User!]!
        }
        """;

    private const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: ID! @is(field: "id")): Author
          productById(id: ID! @is(field: "id")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    private const string ProductsExtensionSdl =
        """
        extend type Query {
          productById(id: ID! @is(field: "id")): Product
        }
        """;
}
