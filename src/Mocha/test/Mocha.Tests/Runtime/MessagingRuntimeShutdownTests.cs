using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public sealed class MessagingRuntimeShutdownTests
{
    [Fact]
    public async Task StopAsync_Should_CancelEventHandler_When_HostStops()
    {
        // arrange
        var probe = new HandlerProbe();
        var logs = new LogRecorder();
        await using var provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddLogging(b => b.AddProvider(logs))
            .AddMessageBus()
            .AddEventHandler<EventHandler>()
            .AddInMemory(t => t.Queue("events").Handler<EventHandler>())
            .Services.BuildServiceProvider();
        var hostedService = await StartAsync(provider);
        await PublishAsync(provider);
        await WaitAsync(probe.Started);

        // act
        await hostedService.StopAsync(TestContext.Current.CancellationToken);
        await WaitAsync(probe.Completed);

        // assert
        Assert.True(Assert.Single(probe.Tokens).IsCancellationRequested);
        Assert.Empty(logs.Entries);
    }

    [Fact]
    public async Task StopAsync_Should_CancelBatchHandler_When_HostStops()
    {
        // arrange
        var probe = new HandlerProbe();
        var logs = new LogRecorder();
        await using var provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddLogging(b => b.AddProvider(logs))
            .AddMessageBus()
            .AddBatchHandler<BatchHandler>(o => o.MaxBatchSize = 1)
            .AddInMemory(t => t.Queue("batches").Handler<BatchHandler>())
            .Services.BuildServiceProvider();
        var hostedService = await StartAsync(provider);
        await PublishAsync(provider);
        await WaitAsync(probe.Started);

        // act
        await hostedService.StopAsync(TestContext.Current.CancellationToken);
        await WaitAsync(probe.Completed);

        // assert
        Assert.True(Assert.Single(probe.Tokens).IsCancellationRequested);
        Assert.Empty(logs.Entries);
    }

    [Fact]
    public async Task StopAsync_Should_CancelOnlyStoppedEndpointBatch_When_EndpointsShareHandler()
    {
        // arrange
        var probe = new HandlerProbe();
        await using var provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddMessageBus()
            .AddBatchHandler<BatchHandler>(o => o.MaxBatchSize = 1)
            .AddInMemory(t =>
            {
                t.Queue("first").Handler<BatchHandler>();
                t.Queue("second").Handler<BatchHandler>();
            })
            .Services.BuildServiceProvider();
        await StartAsync(provider);
        await PublishAsync(provider);
        await WaitAsync(probe.Started);
        await WaitAsync(probe.Started);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var endpoints = runtime.Transports.Single().ReceiveEndpoints;
        var first = endpoints.Single(e => e.Name == "first");
        var second = endpoints.Single(e => e.Name == "second");

        // act
        await first.StopAsync(runtime, TestContext.Current.CancellationToken);
        await WaitAsync(probe.Completed);

        // assert
        Assert.Single(probe.Tokens, token => token.IsCancellationRequested);
        Assert.True(second.IsStarted);
    }

    private static async Task<IHostedService> StartAsync(IServiceProvider provider)
    {
        var hostedService = provider.GetServices<IHostedService>().OfType<MessagingRuntimeHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        return hostedService;
    }

    private static async Task PublishAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new TestEvent(), TestContext.Current.CancellationToken);
    }

    private static async Task WaitAsync(SemaphoreSlim signal)
    {
        if (!await signal.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
        {
            throw new TimeoutException("The handler did not signal in time.");
        }
    }

    public sealed class TestEvent;

    public sealed class HandlerProbe
    {
        public ConcurrentQueue<CancellationToken> Tokens { get; } = new();

        public SemaphoreSlim Started { get; } = new(0);

        public SemaphoreSlim Completed { get; } = new(0);

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            Tokens.Enqueue(cancellationToken);
            Started.Release();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                Completed.Release();
            }
        }
    }

    public sealed class EventHandler(HandlerProbe probe) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
            => probe.RunAsync(cancellationToken);
    }

    public sealed class BatchHandler(HandlerProbe probe) : IBatchEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestEvent> messages, CancellationToken cancellationToken)
            => probe.RunAsync(cancellationToken);
    }
}
