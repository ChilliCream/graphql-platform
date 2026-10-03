using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public sealed class BatchShutdownTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task StopAsync_Should_LetBatchFinish_When_AnotherEndpointOfTheBatchKeepsRunning()
    {
        // arrange
        var gate = new HandlerGate(failAfterCancellation: false);
        await using var provider = await CreateBusAsync(gate);
        await PublishAsync(provider);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.Single();
        var endpoint = transport.ReceiveEndpoints.Single(e => e.Name == "batch-a");

        // act
        await endpoint.StopAsync(runtime, new CancellationToken(canceled: true));
        var cancelledByStop = gate.Cancelled.Task.IsCompleted;
        gate.Release.TrySetResult();
        await gate.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            BatchSize = gate.BatchSize,
            CancelledByStop = cancelledByStop,
            Faults = await CountFaultsAsync(transport),
            StoppedEndpoint = endpoint.IsStarted,
            OtherEndpoint = transport.ReceiveEndpoints.Single(e => e.Name == "batch-b").IsStarted
        }.MatchInlineSnapshot(
            """
            {
              "BatchSize": 2,
              "CancelledByStop": false,
              "Faults": 0,
              "StoppedEndpoint": false,
              "OtherEndpoint": true
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_Should_CancelBatchWithoutFaults_When_AllEndpointsOfTheBatchStop(
        bool failAfterCancellation)
    {
        // arrange
        var gate = new HandlerGate(failAfterCancellation);
        await using var provider = await CreateBusAsync(gate);
        await PublishAsync(provider);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        await runtime.StopAsync(new CancellationToken(canceled: true));

        // assert
        new
        {
            BatchSize = gate.BatchSize,
            CancelledByStop = gate.Cancelled.Task.IsCompleted,
            Faults = await CountFaultsAsync(runtime.Transports.Single())
        }.MatchInlineSnapshot(
            """
            {
              "BatchSize": 2,
              "CancelledByStop": true,
              "Faults": 0
            }
            """);
    }

    private static Task<ServiceProvider> CreateBusAsync(HandlerGate gate)
        => new ServiceCollection()
            .AddSingleton(gate)
            .AddMessageBus()
            .AddBatchHandler<GatedBatchHandler>(o => o.MaxBatchSize = 2)
            .AddInMemory(t =>
            {
                t.ModifyOptions(o => o.Shutdown.CancellationGracePeriod = TimeSpan.FromMilliseconds(100));
                t.Queue("batch-a").Handler<GatedBatchHandler>()
                    .FaultEndpoint(new Uri("memory:///q/batch-a_error"));
                t.Queue("batch-b").Handler<GatedBatchHandler>()
                    .FaultEndpoint(new Uri("memory:///q/batch-b_error"));
            })
            .BuildServiceProvider();

    private static async Task PublishAsync(ServiceProvider provider)
    {
        // The event is delivered to both queues, so one batch holds a message of each endpoint.
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new OrderCreated { OrderId = "review" }, CancellationToken.None);
    }

    private static async Task<int> CountFaultsAsync(MessagingTransport transport)
    {
        var topology = (InMemoryMessagingTopology)transport.Topology;
        var faults = 0;

        foreach (var queue in topology.Queues.Where(q => q.Name.EndsWith("_error", StringComparison.Ordinal)))
        {
            using var readCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            try
            {
                await foreach (var item in queue.ConsumeAsync(readCts.Token))
                {
                    faults++;
                    item.Dispose();
                }
            }
            catch (OperationCanceledException) when (readCts.IsCancellationRequested)
            {
                // The queue has no more items.
            }
        }

        return faults;
    }

    public sealed class HandlerGate(bool failAfterCancellation)
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool FailAfterCancellation => failAfterCancellation;

        public int BatchSize { get; set; }
    }

    public sealed class GatedBatchHandler(HandlerGate gate) : IBatchEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(IMessageBatch<OrderCreated> batch, CancellationToken cancellationToken)
        {
            gate.BatchSize = batch.Count;
            gate.Started.TrySetResult();

            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled.TrySetResult();

                if (gate.FailAfterCancellation)
                {
                    throw new InvalidOperationException("Handler failed after cancellation.");
                }

                throw;
            }

            gate.Completed.TrySetResult();
        }
    }
}
