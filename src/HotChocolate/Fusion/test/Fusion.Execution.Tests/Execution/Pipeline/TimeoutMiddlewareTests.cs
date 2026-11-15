using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public class TimeoutMiddlewareTests : FusionTestBase
{
    private const string SchemaText =
        """
        type Query {
            field: String
        }
        """;

    [Fact]
    public async Task InvokeAsync_Should_ReturnTimeoutError_When_ExecutionTimeoutElapses()
    {
        // arrange
        await using var services = CreateServices(
            TimeSpan.FromMilliseconds(100),
            builder => builder.UseRequest(
                (_, _) => async context => await Task.Delay(Timeout.Infinite, context.RequestAborted),
                after: WellKnownRequestMiddleware.TimeoutMiddleware,
                allowMultiple: true));
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The request exceeded the configured timeout of `00:00:00.1000000`.",
                  "extensions": {
                    "code": "HC0045"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnCanceledError_When_RequestIsAborted()
    {
        // arrange
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var services = CreateServices(
            TimeSpan.FromSeconds(30),
            builder => builder.UseRequest(
                (_, _) => context => WaitForCancellationAsync(context, started),
                after: WellKnownRequestMiddleware.TimeoutMiddleware,
                allowMultiple: true));
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var requestAborted = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        // act
        var execution = executor.ExecuteAsync("{ __typename }", requestAborted.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await requestAborted.CancelAsync();
        await using var result = await execution;

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The GraphQL request execution was canceled.",
                  "extensions": {
                    "code": "HC0049"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnCanceledError_When_SchemaIsShutDown()
    {
        // arrange
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var services = CreateServices(
            TimeSpan.FromSeconds(30),
            builder => builder.UseRequest(
                (_, _) => context => WaitForCancellationAsync(context, started),
                after: WellKnownRequestMiddleware.TimeoutMiddleware,
                allowMultiple: true));
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var execution = executor.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await executor.Schema.Features.GetRequired<SchemaCancellationFeature>().DisposeAsync();
        await using var result = await execution;

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The GraphQL request execution was canceled.",
                  "extensions": {
                    "code": "HC0049"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task InvokeAsync_Should_ExecuteLaterRequests_When_PreviousRequestTimedOut()
    {
        // arrange
        await using var services = CreateServices(
            TimeSpan.FromSeconds(1),
            builder => builder.UseRequest(
                (_, _) => async context =>
                {
                    if (context.Request.OperationName == "Slow")
                    {
                        await Task.Delay(Timeout.Infinite, context.RequestAborted);
                    }

                    await Task.Yield();
                    context.Result = CreateTokenStateResult(context);
                },
                after: WellKnownRequestMiddleware.TimeoutMiddleware,
                allowMultiple: true));
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var cancellationToken = TestContext.Current.CancellationToken;
        using var slowRequest = CreateRequest("Slow");
        using var firstFastRequest = CreateRequest("Fast");
        using var secondFastRequest = CreateRequest("Fast");

        var results = new List<IExecutionResult>();

        try
        {
            // act
            results.Add(await executor.ExecuteAsync(slowRequest, cancellationToken));
            results.Add(await executor.ExecuteAsync(firstFastRequest, cancellationToken));
            results.Add(await executor.ExecuteAsync(secondFastRequest, cancellationToken));

            // assert
            results.MatchInlineSnapshots(
            [
                """
                {
                  "errors": [
                    {
                      "message": "The request exceeded the configured timeout of `00:00:01`.",
                      "extensions": {
                        "code": "HC0045"
                      }
                    }
                  ]
                }
                """,
                """
                {
                  "extensions": {
                    "canceled": false
                  }
                }
                """,
                """
                {
                  "extensions": {
                    "canceled": false
                  }
                }
                """
            ]);
        }
        finally
        {
            foreach (var result in results)
            {
                await result.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task InvokeAsync_Should_RestoreRequestAborted_When_ResultIsNotStream()
    {
        // arrange
        CancellationToken outerToken = default;
        CancellationToken innerToken = default;
        CancellationToken outerTokenAfterNext = default;
        await using var services = CreateServices(
            TimeSpan.FromSeconds(30),
            builder => builder
                .UseRequest(
                    (_, next) => async context =>
                    {
                        outerToken = context.RequestAborted;
                        await next(context);
                        outerTokenAfterNext = context.RequestAborted;
                    },
                    before: WellKnownRequestMiddleware.TimeoutMiddleware,
                    allowMultiple: true)
                .UseRequest(
                    (_, _) => context =>
                    {
                        innerToken = context.RequestAborted;
                        context.Result = CreateTokenStateResult(context);
                        return default;
                    },
                    after: WellKnownRequestMiddleware.TimeoutMiddleware,
                    allowMultiple: true));
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var requestAborted = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ __typename }", requestAborted.Token);

        // assert
        Assert.Equal(requestAborted.Token, outerToken);
        Assert.NotEqual(outerToken, innerToken);
        Assert.Equal(outerToken, outerTokenAfterNext);
    }

    [Fact]
    public async Task InvokeAsync_Should_KeepStreamTokenLinkedToRequest_When_ResultIsStream()
    {
        // arrange
        CancellationToken streamToken = default;
        CancellationToken laterRequestToken = default;
        await using var services = CreateServices(
            TimeSpan.FromMilliseconds(100),
            builder => builder.UseRequest(
                (_, _) => context =>
                {
                    if (context.Request.OperationName == "Stream")
                    {
                        streamToken = context.RequestAborted;
                        context.Result = new ResponseStream(
                            ReadEmptyStreamAsync,
                            ExecutionResultKind.DeferredResult);
                        return default;
                    }

                    laterRequestToken = context.RequestAborted;
                    context.Result = CreateTokenStateResult(context);
                    return default;
                },
                after: WellKnownRequestMiddleware.TimeoutMiddleware,
                allowMultiple: true));
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var cancellationToken = TestContext.Current.CancellationToken;
        using var requestAborted = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var streamRequest = CreateRequest("Stream");
        using var laterRequest = CreateRequest("Later");

        // act
        // the stream outlives the execution timeout and a later request runs while it is open
        await using var stream = await executor.ExecuteAsync(streamRequest, requestAborted.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        await using var laterResult = await executor.ExecuteAsync(laterRequest, cancellationToken);
        var canceledBeforeAbort = streamToken.IsCancellationRequested;
        await requestAborted.CancelAsync();
        var canceledAfterAbort = streamToken.IsCancellationRequested;

        // assert
        Assert.NotEqual(streamToken, laterRequestToken);
        Assert.False(canceledBeforeAbort);
        Assert.True(canceledAfterAbort);
    }

    private static ServiceProvider CreateServices(
        TimeSpan executionTimeout,
        Action<IFusionGatewayBuilder> configure)
    {
        var builder = new ServiceCollection()
            .AddGraphQLGateway()
            .ModifyRequestOptions(o => o.ExecutionTimeout = executionTimeout)
            .AddInMemoryConfiguration(ComposeSchemaDocument(SchemaText));

        configure(builder);

        return builder.Services.BuildServiceProvider();
    }

    private static IOperationRequest CreateRequest(string operationName)
        => OperationRequestBuilder.New()
            .SetDocument($"query {operationName} {{ __typename }}")
            .SetOperationName(operationName)
            .Build();

    private static OperationResult CreateTokenStateResult(RequestContext context)
        => new(
            ImmutableOrderedDictionary<string, object?>.Empty.Add(
                "canceled",
                context.RequestAborted.IsCancellationRequested));

    private static async ValueTask WaitForCancellationAsync(
        RequestContext context,
        TaskCompletionSource started)
    {
        started.TrySetResult();
        await Task.Delay(Timeout.Infinite, context.RequestAborted);
    }

    private static async IAsyncEnumerable<OperationResult> ReadEmptyStreamAsync()
    {
        await Task.CompletedTask;
        yield break;
    }
}
