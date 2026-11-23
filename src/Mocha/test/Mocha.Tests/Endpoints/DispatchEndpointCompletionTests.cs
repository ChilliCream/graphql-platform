using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class DispatchEndpointCompletionTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ExecuteAsync_Should_DispatchOnceCompleted_When_IssuedBeforeEndpointIsCompleted()
    {
        // arrange
        await using var provider = CreateProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = AddUncompletedEndpoint(runtime);
        var context = CreateContext(
            provider,
            runtime,
            endpoint,
            "before-completion",
            TestContext.Current.CancellationToken);

        // act
        var dispatch = endpoint.ExecuteAsync(context).AsTask();
        var completedBeforeEndpoint = dispatch.IsCompleted;
        endpoint.DiscoverTopology(runtime);
        endpoint.Complete(runtime);
        await dispatch.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        using var item = await ReadQueueItemAsync(endpoint);

        // assert
        Assert.False(completedBeforeEndpoint);
        Assert.Equal("before-completion", item.Envelope.MessageId);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ThrowOperationCanceled_When_CancelledBeforeEndpointIsCompleted()
    {
        // arrange
        await using var provider = CreateProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = AddUncompletedEndpoint(runtime);
        using var cts = new CancellationTokenSource();
        var cancelled = CreateContext(provider, runtime, endpoint, "cancelled", cts.Token);
        var dispatch = endpoint.ExecuteAsync(cancelled).AsTask();

        // act
        await cts.CancelAsync();
        // the dispatch must settle before the endpoint is completed
        var outcome = dispatch.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await outcome.ConfigureAwait(
            ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
        endpoint.DiscoverTopology(runtime);
        endpoint.Complete(runtime);
        var next = CreateContext(
            provider,
            runtime,
            endpoint,
            "after-completion",
            TestContext.Current.CancellationToken);
        await endpoint.ExecuteAsync(next);
        using var item = await ReadQueueItemAsync(endpoint);

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => outcome);
        Assert.Equal("after-completion", item.Envelope.MessageId);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddMessageBus().AddInMemory();
        return services.BuildServiceProvider();
    }

    private static InMemoryDispatchEndpoint AddUncompletedEndpoint(IMessagingRuntime runtime)
    {
        var transport = runtime.Transports.Single();
        var configuration = new InMemoryDispatchEndpointConfiguration { QueueName = "startup", Name = "q/startup" };
        return (InMemoryDispatchEndpoint)transport.AddEndpoint(runtime, configuration);
    }

    private static DispatchContext CreateContext(
        IServiceProvider services,
        IMessagingRuntime runtime,
        DispatchEndpoint endpoint,
        string messageId,
        CancellationToken cancellationToken)
    {
        var context = new DispatchContext { MessageId = messageId, Message = new StartupMessage() };
        var messageType = runtime.GetMessageType(typeof(StartupMessage));
        context.Initialize(services, endpoint, runtime, messageType, cancellationToken);
        return context;
    }

    private static async Task<InMemoryQueueItem> ReadQueueItemAsync(InMemoryDispatchEndpoint endpoint)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(s_timeout);
        await using var messages = endpoint.Queue!.ConsumeAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await messages.MoveNextAsync());
        return messages.Current;
    }

    public sealed record StartupMessage;
}
