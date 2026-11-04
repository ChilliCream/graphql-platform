using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class PlannerOptionsConfigurationTests : FusionTestBase
{
    private const string SourceSchema = "type Query { field: String! }";

    [Fact]
    public async Task PlannerOptions_Should_UseAllSettings_When_Configured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLGateway().AddInMemoryConfiguration(
            ComposeSchemaDocument(SourceSchema),
            Settings("""
                { "planner": { "depthWeight": 2.5, "operationWeight": 3.5,
                  "enableRequestGrouping": false, "enableDefer": false,
                  "inlineUnlabeledDeferFragments": false, "mergePolicy": "Balanced",
                  "enableEntityReuseSplitting": false, "excessFanoutWeight": 4.5,
                  "fanoutPenaltyThreshold": 6, "maxPlanningTime": "00:00:05",
                  "maxExpandedNodes": 7, "maxQueueSize": 8, "maxGeneratedOptionsPerWorkItem": 9 } }
                """));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var options = executor.Schema.Services.GetRequiredService<OperationPlannerOptions>();

        // assert
        Assert.Equal(
            (2.5, 3.5, false, false, false, OperationMergePolicy.Balanced, false, 4.5, 6,
                (TimeSpan?)TimeSpan.FromSeconds(5), 7, 8, 9),
            (options.DepthWeight, options.OperationWeight, options.EnableRequestGrouping, options.EnableDefer,
                options.InlineUnlabeledDeferFragments, options.MergePolicy, options.EnableEntityReuseSplitting,
                options.ExcessFanoutWeight, options.FanoutPenaltyThreshold, options.MaxPlanningTime,
                options.MaxExpandedNodes, options.MaxQueueSize, options.MaxGeneratedOptionsPerWorkItem));
    }

    [Fact]
    public async Task PlannerOptions_Should_UseDefaults_When_SettingsAreAbsent()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLGateway().AddInMemoryConfiguration(ComposeSchemaDocument(SourceSchema));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var options = executor.Schema.Services.GetRequiredService<OperationPlannerOptions>();

        // assert
        Assert.Equal(
            (15.0, 1.5, true, true, true, OperationMergePolicy.Aggressive, true, 3.0, 8,
                null, null, null, null),
            (options.DepthWeight, options.OperationWeight, options.EnableRequestGrouping, options.EnableDefer,
                options.InlineUnlabeledDeferFragments, options.MergePolicy, options.EnableEntityReuseSplitting,
                options.ExcessFanoutWeight, options.FanoutPenaltyThreshold, options.MaxPlanningTime,
                options.MaxExpandedNodes, options.MaxQueueSize, options.MaxGeneratedOptionsPerWorkItem));
    }

    [Fact]
    public async Task PlannerOptions_Should_ApplySettingsAndCallbacks_When_FusionOptionsAreConfigured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLGateway()
            .ModifyOptions(options => { options.EnableDefer = false; options.EnableEntityReuse = false; })
            .ModifyPlannerOptions(options => options.EnableEntityReuseSplitting = false)
            .AddInMemoryConfiguration(ComposeSchemaDocument(SourceSchema),
                Settings("""{ "planner": { "enableDefer": true, "enableEntityReuseSplitting": true } }"""));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var options = executor.Schema.Services.GetRequiredService<OperationPlannerOptions>();

        // assert
        Assert.Equal((true, false), (options.EnableDefer, options.EnableEntityReuseSplitting));
    }

    [Fact]
    public async Task PlannerOptions_Should_OverrideFusionOptions_When_SettingsAreConfigured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLGateway()
            .ModifyOptions(options => { options.EnableDefer = false; options.EnableEntityReuse = false; })
            .AddInMemoryConfiguration(ComposeSchemaDocument(SourceSchema),
                Settings("""{ "planner": { "enableDefer": true, "enableEntityReuseSplitting": true } }"""));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var options = executor.Schema.Services.GetRequiredService<OperationPlannerOptions>();

        // assert
        Assert.Equal((true, true), (options.EnableDefer, options.EnableEntityReuseSplitting));
    }

    [Theory]
    [InlineData("mergePolicy", "\"invalid\"")]
    [InlineData("mergePolicy", "1")]
    [InlineData("depthWeight", "\"1\"")]
    [InlineData("fanoutPenaltyThreshold", "1.5")]
    [InlineData("maxPlanningTime", "\"00:00:00\"")]
    [InlineData("maxQueueSize", "0")]
    [InlineData("enableDefer", "null")]
    public async Task PlannerOptions_Should_RejectInvalidSettings_When_Parsing(string name, string value)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLGateway().AddInMemoryConfiguration(
            ComposeSchemaDocument(SourceSchema), Settings($$"""{ "planner": { "{{name}}": {{value}} } }"""));

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await services.BuildServiceProvider().GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        // assert
        Assert.Contains($"$.planner.{name}", exception.Message);
    }

    private static JsonDocumentOwner Settings(string json) => new(JsonDocument.Parse(json));
}
