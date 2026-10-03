using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public sealed class ProcessingCancellationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task StopAsync_Should_SendReply_When_HandlerCompletesAfterCancellation()
    {
        // arrange
        var state = new CancellationState();
        var builder = new ServiceCollection()
            .AddSingleton(state)
            .AddMessageBus()
            .AddRequestHandler<CompletingRequestHandler>()
            .AddInMemory();
        builder.ConfigureMessageBus(b => b.UseReceive(new ReceiveMiddlewareConfiguration(
            (_, next) => async context =>
            {
                await next(context);

                if (context.Endpoint.Kind != ReceiveEndpointKind.Reply)
                {
                    state.ReceiveCancelled = context.CancellationToken.IsCancellationRequested;
                }
            },
            "CancellationProbe")));
        await using var provider = await builder.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.Single();
        var endpoint = transport.ReceiveEndpoints.Single(e => e != transport.ReplyReceiveEndpoint);
        using var scope = provider.CreateScope();
        var response = scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .RequestAsync(new TestRequest(), TestContext.Current.CancellationToken).AsTask();
        await state.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        await endpoint.StopAsync(runtime, new CancellationToken(canceled: true));
        var result = await response.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            state.HandlerCancelled,
            state.ReceiveCancelled,
            result.Value
        }.MatchInlineSnapshot(
            """
            {
              "HandlerCancelled": true,
              "ReceiveCancelled": true,
              "Value": "completed"
            }
            """);
    }

    public sealed class TestRequest : IEventRequest<TestResponse>;

    public sealed record TestResponse(string Value);

    public sealed class CancellationState
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HandlerCancelled { get; set; }
        public bool ReceiveCancelled { get; set; }
    }

    public sealed class CompletingRequestHandler(CancellationState state) : IEventRequestHandler<TestRequest, TestResponse>
    {
        public async ValueTask<TestResponse> HandleAsync(TestRequest message, CancellationToken cancellationToken)
        {
            state.Started.TrySetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.HandlerCancelled = true;
            }

            return new TestResponse("completed");
        }
    }
}
