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

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxFieldCost_Should_ValidateDomain_When_SetOnFusionOptions(
        double value,
        bool isValid)
    {
        // arrange
        var options = new FusionCostOptions();

        // act
        var exception = Record.Exception(() => options.MaxFieldCost = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.MaxFieldCost);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Equal(1_000, options.MaxFieldCost);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxTypeCost_Should_ValidateDomain_When_SetOnFusionOptions(
        double value,
        bool isValid)
    {
        // arrange
        var options = new FusionCostOptions();

        // act
        var exception = Record.Exception(() => options.MaxTypeCost = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.MaxTypeCost);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Equal(10_000, options.MaxTypeCost);
        }
    }

    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(null, true)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void MaxResponseSize_Should_ValidateDomain_When_SetOnFusionOptions(
        double? value,
        bool isValid)
    {
        // arrange
        var options = new FusionCostOptions();

        // act
        var exception = Record.Exception(() => options.MaxResponseSize = value);

        // assert
        if (isValid)
        {
            Assert.Null(exception);
            Assert.Equal(value, options.MaxResponseSize);
        }
        else
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
            Assert.Null(options.MaxResponseSize);
        }
    }
}
