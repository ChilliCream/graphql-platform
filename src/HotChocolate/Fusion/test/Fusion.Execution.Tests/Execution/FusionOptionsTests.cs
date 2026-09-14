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

    [Fact]
    public void Clone_CopiesConfiguredValues()
    {
        // arrange
        var options = new FusionOptions
        {
            EvictionTimeout = TimeSpan.FromSeconds(90),
            OperationDocumentCacheSize = 1024,
            EnableDefer = false,
            EnableObjectDeprecation = true
        };

        // act
        var clone = options.Clone();

        // assert
        clone.MatchInlineSnapshot(
            """
            {
              "EvictionTimeout": "00:01:30",
              "OperationExecutionPlanCacheSize": 256,
              "OperationExecutionPlanCacheDiagnostics": null,
              "OperationDocumentCacheSize": 1024,
              "PathSegmentLocalPoolCapacity": 64,
              "LazyInitialization": false,
              "NodeIdSerializerFormat": "Base64",
              "ApplySerializeAsToScalars": false,
              "EnableDefer": false,
              "EnableObjectDeprecation": true,
              "EnableOptInFeatures": false,
              "EnableEmptySelectionSets": false,
              "EnableSemanticIntrospection": true
            }
            """);
    }
}
