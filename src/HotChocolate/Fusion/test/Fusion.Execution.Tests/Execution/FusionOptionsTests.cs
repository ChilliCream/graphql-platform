using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionOptionsTests : FusionTestBase
{
    /// <summary>
    /// Verifies that setting <c>EnableOptInFeatures</c> via <c>ModifyOptions</c> results in
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
            .ModifyOptions(o => o.EnableOptInFeatures = true)
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
