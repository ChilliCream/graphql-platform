using CookieCrumble;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion.Composition;

public sealed class DemoIntegrationTests
{
    [Fact]
    public async Task Accounts_And_Reviews()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer();

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

        var composer = new FusionGraphComposer();

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

    private const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: Int! @is(field: "id")): User!
          usersById(ids: [Int!]! @is(field: "id")): [User!]!
        }
        """;

    private const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: Int! @is(field: "id")): Author
          productById(upc: Int! @is(field: "upc")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    private const string ProductsExtensionSdl =
        """
        extend type Query {
          productById(upc: Int! @is(field: "upc")): Product
        }
        """;
}
