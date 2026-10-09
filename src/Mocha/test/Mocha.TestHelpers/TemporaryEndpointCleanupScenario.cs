using System.Diagnostics;

namespace Mocha.TestHelpers;

public readonly record struct TemporaryEndpointResourceState(
    bool QueueExists,
    bool BindingExists);

public readonly record struct TemporaryEndpointCleanupResult(
    bool MessageDelivered,
    TemporaryEndpointResourceState BeforeStop,
    TemporaryEndpointResourceState AfterStop);

public static class TemporaryEndpointCleanupScenario
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(100);

    public static async Task<TemporaryEndpointCleanupResult> ExecuteAsync(
        IMessageBus messageBus,
        MessageRecorder recorder,
        Func<CancellationToken, Task<TemporaryEndpointResourceState>> inspectResources,
        Func<CancellationToken, ValueTask> stopTransport,
        CancellationToken cancellationToken)
    {
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "TEMPORARY-ENDPOINT-CLEANUP" },
            cancellationToken);

        var messageDelivered = await recorder.WaitAsync(s_timeout);
        var beforeStop = await inspectResources(cancellationToken);

        await stopTransport(cancellationToken);

        var afterStop = await WaitForStateAsync(
            inspectResources,
            new TemporaryEndpointResourceState(false, false),
            cancellationToken);

        return new TemporaryEndpointCleanupResult(messageDelivered, beforeStop, afterStop);
    }

    private static async Task<TemporaryEndpointResourceState> WaitForStateAsync(
        Func<CancellationToken, Task<TemporaryEndpointResourceState>> inspectResources,
        TemporaryEndpointResourceState expected,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var current = await inspectResources(cancellationToken);

        while (current != expected && Stopwatch.GetElapsedTime(started) < s_timeout)
        {
            await Task.Delay(s_pollInterval, cancellationToken);
            current = await inspectResources(cancellationToken);
        }

        return current;
    }
}
