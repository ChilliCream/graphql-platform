using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.DependencyInjection;

public class CoreFusionGatewayBuilderExtensionsOptInFeaturesTests : FusionTestBase
{
    /// <summary>
    /// Verifies that calling <c>EnableOptInFeatures()</c> on the builder results in
    /// <see cref="IFusionSchemaOptions.EnableOptInFeatures"/> being <c>true</c> on the
    /// built schema's feature collection.
    /// </summary>
    [Fact]
    public async Task EnableOptInFeatures_SetsOption()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .EnableOptInFeatures()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String!
                    }
                    """));

        // act
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var options = executor.Schema.Features.Get<IFusionSchemaOptions>();
        Assert.NotNull(options);
        Assert.True(options.EnableOptInFeatures);
    }
}
