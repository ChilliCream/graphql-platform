using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Tests;

public sealed class MessagingTransportLifecycleTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task DisposeAsync_Should_DisposeTransport_When_EndpointStopObservesCancellation()
    {
        // arrange
        var transport = new TestTransport();
        await using var provider = CreateProvider(transport);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var cancelledStops = new ConcurrentQueue<bool>();
        foreach (var endpoint in transport.ReceiveEndpoints.Cast<TestReceiveEndpoint>())
        {
            endpoint.StopCallback = ct =>
            {
                cancelledStops.Enqueue(ct.IsCancellationRequested);
                ct.ThrowIfCancellationRequested();
                return ValueTask.CompletedTask;
            };
        }

        // act
        await runtime.DisposeAsync();

        // assert
        Assert.Equal([true, true, true], cancelledStops);
        Assert.True(transport.IsDisposed);
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_DrainApplicationEndpointsConcurrentlyBeforeReplies_When_EndpointsAreRunning()
    {
        // arrange
        var transport = new TestTransport();
        await using var provider = CreateProvider(transport);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var first = transport.GetEndpoint("first");
        var second = transport.GetEndpoint("second");
        var reply = transport.GetEndpoint("reply");
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.StopCallback = async ct => await releaseFirst.Task.WaitAsync(ct);
        second.StopCallback = async ct => await releaseSecond.Task.WaitAsync(ct);

        // act
        var stop = transport.StopAsync(runtime, TestContext.Current.CancellationToken).AsTask();
        bool replyStayedStarted;
        bool replyWaitedForEveryApplication;
        try
        {
            await Task.WhenAll(first.StopStarted.Task, second.StopStarted.Task)
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            replyStayedStarted = reply.IsStarted && reply.StopCount == 0;
            releaseFirst.TrySetResult();
            await first.StopCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            replyWaitedForEveryApplication = reply.IsStarted && reply.StopCount == 0;
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseSecond.TrySetResult();
            await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.True(replyStayedStarted);
        Assert.True(replyWaitedForEveryApplication);
        Assert.Equal(["before", "reply", "after"], transport.Stops.Where(name => name is not "first" and not "second"));
        Assert.Equal([false, false, false], transport.ReceiveEndpoints.Select(endpoint => endpoint.IsStarted));
        Assert.False(transport.IsStarted);
    }

    [Theory]
    [InlineData("before")]
    [InlineData("first")]
    [InlineData("reply")]
    [InlineData("after")]
    public async Task StopAsync_Should_AttemptEveryStageAndRethrowFailure_When_StageFails(string stage)
    {
        // arrange
        var failure = new InvalidOperationException("Stop failed.");
        var transport = new TestTransport();
        await using var provider = CreateProvider(transport);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        if (stage == "before")
        {
            transport.BeforeStopFailure = failure;
        }
        else if (stage == "after")
        {
            transport.AfterStopFailure = failure;
        }
        else
        {
            transport.GetEndpoint(stage).StopFailure = failure;
        }

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.StopAsync(runtime, TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Same(failure, exception);
        Assert.Equal(["after", "before", "first", "reply", "second"], transport.Stops.Order());
        Assert.True(transport.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_PreserveFirstFailure_When_MultipleStagesFail()
    {
        // arrange
        var firstFailure = new InvalidOperationException("Before stop failed.");
        var transport = new TestTransport();
        await using var provider = CreateProvider(transport);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        transport.BeforeStopFailure = firstFailure;
        transport.AfterStopFailure = new InvalidOperationException("After stop failed.");
        foreach (var endpoint in transport.ReceiveEndpoints.Cast<TestReceiveEndpoint>())
        {
            endpoint.StopFailure = new InvalidOperationException("Endpoint stop failed.");
        }

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.StopAsync(runtime, TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Same(firstFailure, exception);
        Assert.Equal(["after", "before", "first", "reply", "second"], transport.Stops.Order());
        Assert.Equal([true, true, true], transport.ReceiveEndpoints.Select(endpoint => endpoint.IsStarted));
        Assert.True(transport.IsStarted);
    }

    [Fact]
    public async Task StartAsync_Should_StopStartedEndpointsAndRethrow_When_EndpointStartFails()
    {
        // arrange
        var failure = new InvalidOperationException("Start failed.");
        var transport = new TestTransport();
        await using var provider = CreateProvider(transport);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        // the reply endpoint and one application endpoint start, the second application endpoint fails
        transport.StartFailure = (Position: 3, failure);
        var stopTokensCancelled = new ConcurrentQueue<bool>();
        foreach (var endpoint in transport.ReceiveEndpoints.Cast<TestReceiveEndpoint>())
        {
            endpoint.StopCallback = ct =>
            {
                stopTokensCancelled.Enqueue(ct.IsCancellationRequested);
                return ValueTask.CompletedTask;
            };
        }

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.StartAsync(runtime, TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Same(failure, exception);
        Assert.Equal(["before", transport.Starts.ElementAt(1), "reply", "after"], transport.Stops);
        Assert.Equal([true, true], stopTokensCancelled);
        Assert.Equal([false, false, false], transport.ReceiveEndpoints.Select(endpoint => endpoint.IsStarted));
        Assert.False(transport.IsStarted);
    }

    [Fact]
    public async Task StartAsync_Should_RethrowStartFailure_When_StoppingStartedEndpointsFails()
    {
        // arrange
        var failure = new InvalidOperationException("Start failed.");
        var transport = new TestTransport();
        await using var provider = CreateProvider(transport);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        transport.StartFailure = (Position: 3, failure);
        transport.BeforeStopFailure = new InvalidOperationException("Before stop failed.");
        transport.AfterStopFailure = new InvalidOperationException("After stop failed.");
        foreach (var endpoint in transport.ReceiveEndpoints.Cast<TestReceiveEndpoint>())
        {
            endpoint.StopFailure = new InvalidOperationException("Endpoint stop failed.");
        }

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.StartAsync(runtime, TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Same(failure, exception);
        Assert.Equal(["before", transport.Starts.ElementAt(1), "reply", "after"], transport.Stops);
        Assert.False(transport.IsStarted);
    }

    private static ServiceProvider CreateProvider(TestTransport transport)
    {
        var builder = new ServiceCollection().AddMessageBus();
        builder.ConfigureMessageBus(bus => bus.AddTransport(transport));
        return builder.Services.BuildServiceProvider();
    }

    private sealed class TestTransport : MessagingTransport
    {
        public ConcurrentQueue<string> Starts { get; } = new();

        public ConcurrentQueue<string> Stops { get; } = new();

        public (int Position, Exception Exception)? StartFailure { get; set; }

        public bool IsDisposed { get; private set; }

        public Exception? BeforeStopFailure { get; set; }

        public Exception? AfterStopFailure { get; set; }

        public override MessagingTopology Topology { get; }

        public TestTransport()
        {
            Topology = new TestTopology(this);
        }

        public TestReceiveEndpoint GetEndpoint(string name)
            => ReceiveEndpoints.Cast<TestReceiveEndpoint>().Single(endpoint => endpoint.Name == name);

        public override ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }

        public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
            => new TestTransportConfiguration
            {
                Name = "test",
                Schema = "test",
                RoutingStrategyFactory = static _ => new TestRoutingStrategy(),
                ReceiveEndpoints =
                [
                    new ReceiveEndpointConfiguration { Name = "first" },
                    new ReceiveEndpointConfiguration { Name = "second" },
                    new ReceiveEndpointConfiguration { Name = "reply", Kind = ReceiveEndpointKind.Reply }
                ]
            };

        protected override ReceiveEndpoint CreateReceiveEndpoint() => new TestReceiveEndpoint(this);

        protected override DispatchEndpoint CreateDispatchEndpoint() => throw new NotSupportedException();

        protected override ValueTask OnBeforeStopAsync(CancellationToken cancellationToken)
        {
            Stops.Enqueue("before");
            var failure = BeforeStopFailure;
            BeforeStopFailure = null;
            return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
        }

        protected override ValueTask OnAfterStopAsync(CancellationToken cancellationToken)
        {
            Stops.Enqueue("after");
            var failure = AfterStopFailure;
            AfterStopFailure = null;
            return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
        }
    }

    private sealed class TestReceiveEndpoint(TestTransport transport) : ReceiveEndpoint(transport)
    {
        public TaskCompletionSource StopStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource StopCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Func<CancellationToken, ValueTask> StopCallback { get; set; } = static _ => ValueTask.CompletedTask;

        public Exception? StopFailure { get; set; }

        public int StopCount { get; private set; }

        protected override void OnInitialize(
            IMessagingConfigurationContext context,
            ReceiveEndpointConfiguration configuration)
        { }

        protected override ValueTask OnStartAsync(
            IMessagingRuntimeContext context,
            CancellationToken cancellationToken)
        {
            transport.Starts.Enqueue(Name);

            return transport.StartFailure is { } failure && failure.Position == transport.Starts.Count
                ? ValueTask.FromException(failure.Exception)
                : ValueTask.CompletedTask;
        }

        protected override async ValueTask OnStopAsync(
            IMessagingRuntimeContext context,
            CancellationToken cancellationToken)
        {
            transport.Stops.Enqueue(Name);
            StopCount++;
            StopStarted.TrySetResult();
            await StopCallback(cancellationToken);
            var failure = StopFailure;
            StopFailure = null;
            if (failure is not null)
            {
                throw failure;
            }

            StopCompleted.TrySetResult();
        }
    }

    private sealed class TestTransportConfiguration : MessagingTransportConfiguration;

    private sealed class TestTopology(MessagingTransport transport) : MessagingTopology(transport, new Uri("test:///"));

    private sealed class TestRoutingStrategy : RoutingStrategy
    {
        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            OutboundRoute route)
            => null;

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            Uri address)
            => null;

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            InboundRoute route)
            => null;
    }
}
