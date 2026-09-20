using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationVariableCoercionMiddlewareTests : FusionTestBase
{
    private const string SchemaText =
        """
        type Query {
          field(input: String!): String
        }
        """;

    private const string OperationText =
        "query test($input: String!) { field(input: $input) }";

    [Fact]
    public async Task InvokeAsync_Should_CoerceVariables_When_RequestIsWarmup()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .SetVariableValues(new Dictionary<string, object?> { ["input"] = "value" })
            .MarkAsWarmupRequest()
            .Build();

        // act
        var result = await executor.ExecuteAsync(warmupRequest, TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<WarmupExecutionResult>(result);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnNonNullViolation_When_RequiredVariableIsMissing_ForWarmupRequest()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .MarkAsWarmupRequest()
            .Build();

        // act
        var result = await executor.ExecuteAsync(warmupRequest, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, error.Code);
    }

    [Fact]
    public async Task InvokeAsync_Should_CoerceVariables_When_CostValidationHasNoVariablesAndAnalyzerIsEnabled()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var request = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, error.Code);
    }

    [Fact]
    public async Task InvokeAsync_Should_CoerceVariables_When_CostValidationHasNoVariablesAndAnalyzerIsSkipped()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            builder => builder.ModifyCostOptions(options => options.SkipAnalyzer = true));
        var request = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, error.Code);
    }

    [Fact]
    public async Task InvokeAsync_Should_CoerceVariables_When_CostValidationHasNoAnalyzer()
    {
        // arrange
        var executor = await CreateExecutorAsync(includeCostAnalysis: false);
        var request = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, error.Code);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnNonNullViolation_When_RequiredVariableIsMissing()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var request = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, error.Code);
    }

    private async Task<IRequestExecutor> CreateExecutorAsync(
        Func<IFusionGatewayBuilder, IFusionGatewayBuilder>? configure = null,
        bool includeCostAnalysis = true)
    {
        var services = new ServiceCollection();
        var builder = services.AddGraphQLGateway();

        if (includeCostAnalysis)
        {
            builder.UseDefaultPipeline();
        }
        else
        {
            FusionSetupUtilities.ClearPipeline(builder);
            builder
                .UseExceptions()
                .UseDocumentCache()
                .UseDocumentParser()
                .UseDocumentValidation()
                .UseOperationVariableCoercion();
        }

        if (configure is not null)
        {
            builder = configure(builder);
        }

        return await builder
            .AddInMemoryConfiguration(ComposeSchemaDocument(SchemaText))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
