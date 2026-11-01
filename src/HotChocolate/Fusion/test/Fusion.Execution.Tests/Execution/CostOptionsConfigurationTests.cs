using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using CompositionCaseBudgetExceededBehavior = HotChocolate.Fusion.Options.CaseBudgetExceededBehavior;

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
    public async Task CostOptions_Should_UseSchemaValues_When_ComposedSchemaConfiguresThem()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    options =>
                    {
                        options.Merger.MaxFieldCost = 10.5;
                        options.Merger.MaxTypeCost = 20.5;
                        options.Merger.EnforceCostLimits = false;
                        options.Merger.SkipCostAnalyzer = true;
                        options.Merger.MaxResponseSize = 30.5;
                        options.Merger.CostPlanCacheSize = 40;
                        options.Merger.CaseBudget = 50;
                        options.Merger.CaseBudgetExceededBehavior = CompositionCaseBudgetExceededBehavior.Overestimate;
                    },
                    SourceSchema));

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
    public async Task CostOptions_Should_ApplyCallbacks_AfterSchemaValues()
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
                ComposeSchemaDocument(
                    options =>
                    {
                        options.Merger.MaxFieldCost = 10;
                        options.Merger.EnforceCostLimits = false;
                    },
                    SourceSchema));

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
            CreateFusionConfiguration(
                options => options.Merger.MaxFieldCost = 10,
                SourceSchema));
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
            CreateFusionConfiguration(
                options => options.Merger.MaxFieldCost = 20,
                SourceSchema));
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
    public async Task ExecuteAsync_Should_RejectOperation_When_ComposedMaxFieldCostIsExceeded()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    options => options.Merger.MaxFieldCost = 10,
                    """
                    type Query {
                      expensive: String! @cost(weight: "11")
                    }
                    """));
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
}
