using System.Text.Json;
using HotChocolate.CostAnalysis;
using HotChocolate.Caching.Memory;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Fusion.Execution.Nodes;

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

    private const string CaseBudgetSchema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR

        type Query {
          a: Boolean! @cost(weight: "1")
          b: Boolean! @cost(weight: "2")
          c: Boolean! @cost(weight: "4")
        }
        """;

    private const string CaseBudgetOperation =
        """
        query($x: Boolean!, $y: Boolean!, $z: Boolean!) {
          a @include(if: $x)
          b @include(if: $y)
          c @include(if: $z)
        }
        """;

    [Fact]
    public async Task SchemaIndex_Should_UseDefaultCaseBudget_When_OptionIsNull()
    {
        // arrange
        await using var services = CreateServices();
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var schemaIndex = executor.Schema.Services.GetRequiredService<CostSchemaIndex>();

        // assert
        Assert.Equal(new CostSchemaIndexOptions().CaseBudget, schemaIndex.Options.CaseBudget);
        Assert.Equal(1, schemaIndex.Options.DefaultListSize);
    }

    [Fact]
    public async Task SchemaIndex_Should_UseConfiguredCaseBudget_When_OptionHasValue()
    {
        // arrange
        const int caseBudget = 16;
        await using var services = CreateServices(options => options.CaseBudget = caseBudget);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var schemaIndex = executor.Schema.Services.GetRequiredService<CostSchemaIndex>();

        // assert
        Assert.Equal(caseBudget, schemaIndex.Options.CaseBudget);
    }

    [Fact]
    public async Task ValidateCost_Should_ReturnCoercionError_When_RequiredVariablesAreMissing()
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
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, error.Code);
        Assert.Null(observation.Result);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task ValidateCost_Should_ReturnStateInvalid_When_VariableBatchIsEmpty()
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
            .SetVariableValues("[]")
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        var error = Assert.Single(operationResult.Errors);
        Assert.Equal(ErrorCodes.Execution.CostStateInvalid, error.Code);
        Assert.Equal(
            "The cost analysis requires at least one coerced variable value set.",
            error.Message);
        Assert.False(operationResult.Extensions?.ContainsKey("operationCost") ?? false);
        Assert.Null(observation.Result);
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
        Assert.Single(observation.Result!.Estimates);
        Assert.Equal(3, observation.Result.Estimates[0].TypeCost);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task VariableBatch_Should_Execute_When_SummedTypeCostIsWithinLimit()
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
        using var variables = JsonDocument.Parse("""[{ "n": 4 }, { "n": 4 }]""");
        using var request = VariableBatchRequest.FromSourceText(ItemsQuery, variables);

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.Equal(2, observation.Result!.Estimates.Length);
        Assert.Equal(1, observation.DownstreamCalls);
    }

    [Fact]
    public async Task Request_Should_RejectBeforePlanning_When_SummedTypeCostExceedsLimit()
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
        using var variables = JsonDocument.Parse("""[{ "n": 4 }, { "n": 5 }]""");
        using var request = VariableBatchRequest.FromSourceText(ItemsQuery, variables);

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed type cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "typeCost": 11,
                    "maxTypeCost": 10
                  }
                }
              ]
            }
            """);
        Assert.Equal(2, observation.Result!.Estimates.Length);
        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        var costPlanCache = executor.Schema.Services.GetRequiredService<Cache<CostPlan>>();
        Assert.Equal(0, operationPlanCache.Count);
        Assert.Equal(1, costPlanCache.Count);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task VariableBatch_Should_PrioritizeFieldCost_When_SummedFieldAndTypeCostExceedLimits()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = 1;
                options.MaxTypeCost = 10;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var variables = JsonDocument.Parse("""[{ "n": 4 }, { "n": 5 }]""");
        using var request = VariableBatchRequest.FromSourceText(ItemsQuery, variables);

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed field cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "fieldCost": 2,
                    "maxFieldCost": 1
                  }
                }
              ]
            }
            """);
        Assert.Equal(2, observation.Result!.Estimates.Length);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task VariableBatch_Should_RejectWholeRequest_When_OneSetExceedsTypeCostLimit()
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
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed type cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "typeCost": 1003,
                    "maxTypeCost": 10
                  }
                }
              ]
            }
            """);
        Assert.Equal(2, observation.Result!.Estimates.Length);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task VariableBatch_Should_RejectWholeRequest_When_OneSetExceedsResponseSizeLimit()
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
        using var variables = JsonDocument.Parse("""[{ "n": 1 }, { "n": 1000 }]""");
        using var request = VariableBatchRequest.FromSourceText(ItemsQuery, variables);

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed response size was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "maxResponseSize": 1001,
                    "maxAllowedResponseSize": 100
                  }
                }
              ]
            }
            """);
        Assert.Equal(2, observation.Result!.Estimates.Length);
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

    [Fact]
    public async Task RequestCostOptions_Should_OverrideGatewayLimitUpward_When_RequestRaisesFieldCostLimit()
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
        using var request = OperationRequestBuilder.New()
            .SetDocument("{ costlyLeaf }")
            .ModifyCostOptions((FusionCostOptions o) => o.MaxFieldCost = 5000)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.Equal(1, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_OverrideGatewayLimitDownward_When_RequestLowersFieldCostLimit()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = double.PositiveInfinity;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument("{ costlyLeaf }")
            .ModifyCostOptions((FusionCostOptions o) => o.MaxFieldCost = 1)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed field cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "fieldCost": 2000,
                    "maxFieldCost": 1
                  }
                }
              ]
            }
            """);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_UseGatewayLimit_When_RequestDoesNotModifyCostOptions()
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
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_FailFast_When_MaxResponseSizeIsSetButGatewayDisablesIt()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(null, observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(ItemsQuery)
            .SetVariableValues(new Dictionary<string, object?> { ["n"] = 1 })
            .ModifyCostOptions((FusionCostOptions o) => o.MaxResponseSize = 100)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.ResponseSizeAnalysisNotEnabled, error.Code);
        Assert.Equal(
            "The request cost options set MaxResponseSize, but the schema does not enable the response-size analysis.",
            error.Message);
        Assert.Null(observation.Result);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_BypassAnalysis_When_RequestSkipsAnalyzer()
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
            .SetDocument("{ costlyLeaf }")
            .ModifyCostOptions((FusionCostOptions o) => o.SkipAnalyzer = true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.Null(observation.Result);
        Assert.Equal(1, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_OverrideGatewayLimitDownward_When_RequestLowersMaxResponseSizeLimit()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = double.PositiveInfinity;
                options.MaxResponseSize = 2000;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(ItemsQuery)
            .SetVariableValues(new Dictionary<string, object?> { ["n"] = 1000 })
            .ModifyCostOptions((FusionCostOptions o) => o.MaxResponseSize = 500)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed response size was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "maxResponseSize": 1001,
                    "maxAllowedResponseSize": 500
                  }
                }
              ]
            }
            """);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_OverrideGatewayLimitUpward_When_RequestRaisesMaxResponseSizeLimit()
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
            .ModifyCostOptions((FusionCostOptions o) => o.MaxResponseSize = 2000)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.Equal(1, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_InheritGatewayFieldCost_When_ModifierOnlyTouchesTypeCost()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = 1;
                options.MaxTypeCost = double.PositiveInfinity;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument("{ costlyLeaf }")
            .ModifyCostOptions((FusionCostOptions o) => o.MaxTypeCost = 5000)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(1d, error.Extensions!["maxFieldCost"]);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_ApplyEachModifierInOrder_When_ModifyCostOptionsIsCalledTwice()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = double.PositiveInfinity;
            },
            observation);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(ItemsQuery)
            .SetVariableValues(new Dictionary<string, object?> { ["n"] = 1 })
            .ModifyCostOptions((FusionCostOptions o) =>
            {
                o.MaxFieldCost = 1;
                o.MaxTypeCost = 1;
            })
            .ModifyCostOptions((FusionCostOptions o) => o.MaxFieldCost = 5000)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        // MaxFieldCost = 5000 from the second modifier clears the field-cost limit, so a rejection
        // can only come from MaxTypeCost = 1, which only the first modifier set.
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed type cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "typeCost": 2,
                    "maxTypeCost": 1
                  }
                }
              ]
            }
            """);
        Assert.Equal(0, observation.DownstreamCalls);
    }

    [Fact]
    public async Task RequestCostOptions_Should_ApplyContextModifierOncePerExecution_When_RequestIsExecutedTwice()
    {
        // arrange
        var callCount = 0;
        var services = new ServiceCollection();
        var builder = services
            .AddGraphQLGateway()
            .UseDefaultPipeline();

        builder
            .ModifyCostOptions(options =>
            {
                options.MaxFieldCost = double.PositiveInfinity;
                options.MaxTypeCost = double.PositiveInfinity;
            })
            .UseRequest(
                (_, next) => context =>
                {
                    context.ModifyCostOptions((FusionCostOptions o) =>
                    {
                        callCount++;
                        o.MaxTypeCost = 5000;
                    });
                    return next(context);
                },
                before: WellKnownRequestMiddleware.CostAnalyzerMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, _) => context =>
                {
                    context.Result = CreateProbeResult();
                    return default;
                },
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                allowMultiple: true);

        builder.AddInMemoryConfiguration(ComposeSchemaDocument(defaultListSize: 1, Schema));

        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        using var request = OperationRequestBuilder.New()
            .SetDocument("{ costlyLeaf }")
            .ModifyCostOptions((FusionCostOptions o) => o.MaxFieldCost = 5000)
            .Build();

        // act
        var firstResult = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var secondResult = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, callCount);
        Assert.Empty(firstResult.ExpectOperationResult().Errors);
        Assert.Empty(secondResult.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task CaseBudgetExceededBehavior_Should_PriceExactly_When_DefaultBehaviorIsUsed()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            o => o.CaseBudget = 0,
            observation,
            CaseBudgetSchema);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(CaseBudgetOperation)
            .SetVariableValues(new Dictionary<string, object?> { ["x"] = true, ["y"] = false, ["z"] = true })
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(5.0, observation.Result!.Estimates[0].FieldCost);
    }

    [Fact]
    public async Task CaseBudgetExceededBehavior_Should_PriceByEnvelope_When_OverestimateIsConfigured()
    {
        // arrange
        var observation = new CostObservation();
        await using var services = CreateServices(
            o =>
            {
                o.CaseBudget = 0;
                o.CaseBudgetExceededBehavior = CaseBudgetExceededBehavior.Overestimate;
            },
            observation,
            CaseBudgetSchema);
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New()
            .SetDocument(CaseBudgetOperation)
            .SetVariableValues(new Dictionary<string, object?> { ["x"] = true, ["y"] = false, ["z"] = true })
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(7.0, observation.Result!.Estimates[0].FieldCost);
    }

    private static ServiceProvider CreateServices(
        Action<FusionCostOptions>? configure = null,
        CostObservation? observation = null,
        string schema = Schema)
    {
        var services = new ServiceCollection();
        var builder = services
            .AddGraphQLGateway()
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

        builder.AddInMemoryConfiguration(ComposeSchemaDocument(defaultListSize: 1, schema));
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
