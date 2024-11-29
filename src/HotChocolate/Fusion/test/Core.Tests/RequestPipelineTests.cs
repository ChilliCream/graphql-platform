using HotChocolate.Execution;
using HotChocolate.Fetching;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Fusion;

public class RequestPipelineTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Custom_Middleware_Runs_Before_DefaultPipeline()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(new[] { demoProject.Accounts.ToConfiguration() });

        var config = new DemoIntegrationTests.HotReloadConfiguration(
            new GatewayConfiguration(
                SchemaFormatter.FormatAsDocument(fusionGraph)));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddFusionGatewayServer()
            .RegisterGatewayConfiguration(_ => config)
            .UseRequest<TestMiddleware>()
            .UseDefaultPipeline()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ __typename }")
                .AddGlobalState("short-circuit", true)
                .Build());

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "result": true
              },
              "extensions": {
                "state": "custom middleware short-circuited"
              }
            }
            """);
    }

    [Fact]
    public async Task Custom_Middleware_Falls_Through_To_DefaultPipeline()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(new[] { demoProject.Accounts.ToConfiguration() });

        var config = new DemoIntegrationTests.HotReloadConfiguration(
            new GatewayConfiguration(
                SchemaFormatter.FormatAsDocument(fusionGraph)));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddFusionGatewayServer()
            .RegisterGatewayConfiguration(_ => config)
            .UseRequest<TestMiddleware>()
            .UseDefaultPipeline()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ __typename }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query"
              }
            }
            """);
    }

    [Fact]
    public async Task Custom_Middleware_Without_DefaultPipeline_No_Other_Middleware_Registered()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(new[] { demoProject.Accounts.ToConfiguration() });

        var config = new DemoIntegrationTests.HotReloadConfiguration(
            new GatewayConfiguration(
                SchemaFormatter.FormatAsDocument(fusionGraph)));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddFusionGatewayServer()
            .RegisterGatewayConfiguration(_ => config)
            .UseRequest<TestMiddleware>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ __typename }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "result": true
              },
              "extensions": {
                "state": "default pipeline didn't run"
              }
            }
            """);
    }

    private class TestMiddleware(RequestDelegate next)
    {
        public async ValueTask InvokeAsync(
            IRequestContext context,
            IBatchDispatcher batchDispatcher)
        {
            if (context.ContextData.ContainsKey("short-circuit"))
            {
                context.Result = OperationResultBuilder.New()
                    .SetData(new Dictionary<string, object?> { ["result"] = true })
                    .AddExtension("state", "custom middleware short-circuited")
                    .Build();
                return;
            }

            await next(context);

            if (context.Result is not IOperationResult)
            {
                context.Result = OperationResultBuilder.New()
                    .SetData(new Dictionary<string, object?> { ["result"] = true })
                    .AddExtension("state", "default pipeline didn't run")
                    .Build();
            }
        }
    }
}
