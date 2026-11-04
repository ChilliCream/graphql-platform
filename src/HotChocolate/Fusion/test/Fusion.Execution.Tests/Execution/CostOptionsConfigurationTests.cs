using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class CostOptionsConfigurationTests : FusionTestBase
{
    private const string SourceSchema =
        """
        type Query {
          field: String!
        }
        """;

    [Fact]
    public async Task CostOptions_Should_UseSettingsValues_When_GatewayConfiguresThem()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(SourceSchema),
                Settings("""
                    { "cost": { "maxFieldCost": 10.5, "maxTypeCost": 20.5,
                      "enforceCostLimits": false, "skipAnalyzer": true,
                      "maxResponseSize": 30.5, "costPlanCacheSize": 40,
                      "caseBudget": 50, "caseBudgetExceededBehavior": "Overestimate" } }
                    """));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var costOptions = executor.Schema.Services.GetRequiredService<FusionCostOptions>();

        // assert
        new
        {
            costOptions.MaxFieldCost,
            costOptions.MaxTypeCost,
            costOptions.EnforceCostLimits,
            costOptions.SkipAnalyzer,
            costOptions.MaxResponseSize,
            costOptions.CostPlanCacheSize,
            costOptions.CaseBudget,
            CaseBudgetExceededBehavior = costOptions.CaseBudgetExceededBehavior?.ToString()
        }.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": 10.5,
              "MaxTypeCost": 20.5,
              "EnforceCostLimits": false,
              "SkipAnalyzer": true,
              "MaxResponseSize": 30.5,
              "CostPlanCacheSize": 40,
              "CaseBudget": 50,
              "CaseBudgetExceededBehavior": "Overestimate"
            }
            """);
    }

    [Fact]
    public async Task CostOptions_Should_ApplyCallbacksAfterSettings_When_CallbacksAreConfigured()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyCostOptions(
                options =>
                {
                    options.MaxFieldCost = 20;
                    options.EnforceCostLimits = true;
                })
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(SourceSchema),
                Settings("""{ "cost": { "maxFieldCost": 10, "enforceCostLimits": false } }"""));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var costOptions = executor.Schema.Services.GetRequiredService<FusionCostOptions>();

        // assert
        new { costOptions.MaxFieldCost, costOptions.EnforceCostLimits }.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": 20.0,
              "EnforceCostLimits": true
            }
            """);
    }

    [Fact]
    public async Task CostOptions_Should_UseDefaults_When_CostOptionsDirectiveIsAbsent()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(ComposeSchemaDocument(SourceSchema));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var costOptions = executor.Schema.Services.GetRequiredService<FusionCostOptions>();

        // assert
        costOptions.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": 1000.0,
              "MaxTypeCost": 10000.0,
              "EnforceCostLimits": true,
              "SkipAnalyzer": false,
              "MaxResponseSize": null,
              "CostPlanCacheSize": 256,
              "CaseBudget": null,
              "CaseBudgetExceededBehavior": null
            }
            """);
    }

    [Fact]
    public async Task CostOptions_Should_Update_When_ConfigurationIsReloaded()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var executorEvictedResetEvent = new ManualResetEventSlim();
        var configurationProvider = new TestFusionConfigurationProvider(
            Configuration(10));
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddConfigurationProvider(_ => configurationProvider);
        var manager = services.BuildServiceProvider().GetRequiredService<FusionRequestExecutorManager>();
        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));
        var firstExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        // act
        configurationProvider.UpdateConfiguration(
            Configuration(20));
        executorEvictedResetEvent.Wait(cts.Token);
        var secondExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        // assert
        new
        {
            FirstMaxFieldCost = firstExecutor.Schema.Services
                .GetRequiredService<FusionCostOptions>()
                .MaxFieldCost,
            SecondMaxFieldCost = secondExecutor.Schema.Services
                .GetRequiredService<FusionCostOptions>()
                .MaxFieldCost
        }.MatchInlineSnapshot(
            """
            {
              "FirstMaxFieldCost": 10.0,
              "SecondMaxFieldCost": 20.0
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RejectOperation_When_ConfiguredMaxFieldCostIsExceeded()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      expensive: String! @cost(weight: "11")
                    }
                    """),
                Settings("""{ "cost": { "maxFieldCost": 10 } }"""));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ expensive }",
            TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum allowed field cost was exceeded.",
                  "extensions": {
                    "code": "HC0047",
                    "fieldCost": 11,
                    "maxFieldCost": 10
                  }
                }
              ]
            }
            """);
    }

    private static JsonDocumentOwner Settings(string json) => new(JsonDocument.Parse(json));

    private static FusionConfiguration Configuration(int maxFieldCost)
        => new(ComposeSchemaDocument(SourceSchema), Settings($$"""{ "cost": { "maxFieldCost": {{maxFieldCost}} } }"""));
}
