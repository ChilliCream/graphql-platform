using System.Text.Json;
using HotChocolate.CostAnalysis;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class CostAnalysisMiddlewareTests : FusionTestBase
{
    private const string Schema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

        type Query {
          items(n: Int!): [Item] @listSize(slicingArguments: ["n"])
          costlyLeaf: String @cost(weight: "2000")
        }

        type Item {
          value: String
        }
        """;

    private const string ItemsQuery =
        """
        query Items($n: Int!) {
          items(n: $n) {
            value
          }
        }
        """;

    [Fact]
    public async Task SchemaSnapshot_Should_UseEngineDefaultCaseBudget_When_OptionIsNull()
    {
        // arrange
        await using var services = CreateServices();
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var snapshot = executor.Schema.Services.GetRequiredService<CostSchemaSnapshot>();

        // assert
        Assert.Equal(new CostEngineOptions().CaseBudget, snapshot.Options.CaseBudget);
        Assert.Equal(1, snapshot.Options.DefaultListSize);
    }

    [Fact]
    public async Task SchemaSnapshot_Should_UseConfiguredCaseBudget_When_OptionHasValue()
    {
        // arrange
        const int caseBudget = 16;
        await using var services = CreateServices(options => options.CaseBudget = caseBudget);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var snapshot = executor.Schema.Services.GetRequiredService<CostSchemaSnapshot>();

        // assert
        Assert.Equal(caseBudget, snapshot.Options.CaseBudget);
    }

    [Fact]
    public async Task ValidateCost_Should_UseStaticBoundWithoutEnforcing_When_VariablesAreOmitted()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = 0;
                options.MaxTypeCost = 0;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(ItemsQuery)
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.True(observation.Result!.IsStaticBound);
        Assert.Single(observation.Result.Estimates);
        Assert.Equal(2d, observation.Result.Estimates[0].TypeCost);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task ValidateCost_Should_EvaluateWithoutEnforcing_When_VariablesAreProvided()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = 0;
                options.MaxTypeCost = 0;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(ItemsQuery)
            .SetVariableValues(new Dictionary<string, object?> { ["n"] = 2 })
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.False(observation.Result!.IsStaticBound);
        Assert.Single(observation.Result.Estimates);
        Assert.Equal(3, observation.Result.Estimates[0].TypeCost);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task Request_Should_RejectWholeBatch_When_AnyVariableSetExceedsLimit()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = 10;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var variables = JsonDocument.Parse("""[{ "n": 1 }, { "n": 1000 }]""");
        using var request = VariableBatchRequest.FromSourceText(ItemsQuery, variables);

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(2, observation.Result!.Estimates.Length);
        Assert.False(observation.Result.IsStaticBound);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task Request_Should_RejectBeforePlanning_When_FieldCostExceedsLimit()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = 1000;
                options.MaxTypeCost = double.PositiveInfinity;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ costlyLeaf }",
            TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(2000d, error.Extensions!["fieldCost"]);
        Assert.Equal(1000d, error.Extensions["maxFieldCost"]);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task Request_Should_RejectBeforePlanning_When_MaxResponseSizeExceedsLimit()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = double.PositiveInfinity;
                options.MaxResponseSize = 100;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(ItemsQuery)
            .SetVariableValues(new Dictionary<string, object?> { ["n"] = 1000 })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(1001d, error.Extensions!["maxResponseSize"]);
        Assert.Equal(100d, error.Extensions["maxAllowedResponseSize"]);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task Request_Should_SkipAnalysis_When_AnalyzerIsDisabled()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options => options.SkipAnalyzer = true,
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ costlyLeaf }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.Null(observation.Result);
        Assert.Equal(1, observation.DownstreamCalls);
    }

    private static ServiceProvider CreateServices(
        Action<FusionCostOptions>? configure = null,
        CostObservation? observation = null)
    {
        var services = new ServiceCollection();
        var builder = services
            .AddGraphQLGateway()
            .ModifyCostOptions(options => options.DefaultListSize = 1)
            .UseDefaultPipeline();

        if (configure is not null)
        {
            builder.ModifyCostOptions(configure);
        }

        if (observation is not null)
        {
            builder
                .UseRequest(
                    (_, next) => context => ObserveAsync(context, next, observation),
                    before: WellKnownRequestMiddleware.CostAnalyzerMiddleware,
                    allowMultiple: true)
                .UseRequest(
                    (_, _) => context =>
                    {
                        observation.DownstreamCalls++;
                        context.Result = CreateProbeResult();
                        return default;
                    },
                    before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                    allowMultiple: true);
        }

        builder.AddInMemoryConfiguration(ComposeSchemaDocument(Schema));
        return services.BuildServiceProvider();
    }

    private static OperationResult CreateProbeResult()
        => new(ImmutableOrderedDictionary<string, object?>.Empty.Add("probe", true));

    private static async ValueTask ObserveAsync(
        RequestContext context,
        RequestDelegate next,
        CostObservation observation)
    {
        await next(context);

        if (context.TryGetCostAnalysisResult(out var result))
        {
            observation.Result = result;
        }

        context.Result ??= CreateProbeResult();
    }

    private sealed class CostObservation
    {
        public CostAnalysisResult? Result { get; set; }

        public int DownstreamCalls { get; set; }
    }
}
