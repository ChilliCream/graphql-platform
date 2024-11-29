using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class RequireTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Require_Scalar_Arguments_No_Overloads()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var composer = new FusionGraphComposer(logFactory: _logFactory);

        var fusionConfig = await composer.ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(),
                demoProject.Reviews.ToConfiguration(),
                demoProject.Products.ToConfiguration(
                    """
                    extend type Query {
                      productById(id: ID! @is(field: "id")): Product
                    }
                    """),
                demoProject.Shipping.ToConfiguration(
                    """
                    extend type Query {
                      productById(id: ID! @is(field: "id")): Product
                    }

                    extend type Product {
                      deliveryEstimate(
                        size: Int! @require(field: "dimension { size }"),
                        weight: Int! @require(field: "dimension { weight }"),
                        zip: String!): DeliveryEstimate!
                    }
                    """),
            });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
}
