using System.Reflection;
using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

/// <summary>
/// A <see cref="ServiceBusClient"/> test double that hands out <see cref="FakeServiceBusSender"/>
/// instances so tests can control send/schedule outcomes per sender without a live namespace.
/// </summary>
internal sealed class FakeServiceBusClient : ServiceBusClient
{
    private readonly Func<int, Exception?> _failureAt;
    private int _senderCount;

    public FakeServiceBusClient(Func<int, Exception?> failureAt)
    {
        _failureAt = failureAt;
    }

    public override string FullyQualifiedNamespace => "fake.servicebus.windows.net";

    public List<FakeServiceBusSender> CreatedSenders { get; } = [];

    public List<CreatedProcessor> CreatedProcessors { get; } = [];

    public List<CreatedSessionProcessor> CreatedSessionProcessors { get; } = [];

    public List<FakeServiceBusReceiver> CreatedReceivers { get; } = [];

    /// <summary>
    /// When set, <see cref="CreateReceiver(string)"/> throws the returned exception instead of
    /// returning a receiver, simulating a failure while acquiring the reply queue receiver.
    /// </summary>
    public Func<string, Exception?>? ReceiverFailure { get; set; }

    public override ServiceBusSender CreateSender(string queueOrTopicName)
    {
        var senderIndex = _senderCount++;
        var sender = new FakeServiceBusSender(queueOrTopicName, () => _failureAt(senderIndex));
        CreatedSenders.Add(sender);
        return sender;
    }

    public override ServiceBusSender CreateSender(string queueOrTopicName, ServiceBusSenderOptions options)
        => CreateSender(queueOrTopicName);

    public override ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions options)
    {
        var processor = new FakeServiceBusProcessor();
        CreatedProcessors.Add(new CreatedProcessor(queueName, options, processor));
        return processor;
    }

    public override ServiceBusSessionProcessor CreateSessionProcessor(
        string queueName,
        ServiceBusSessionProcessorOptions options)
    {
        var processor = new FakeServiceBusSessionProcessor();
        CreatedSessionProcessors.Add(new CreatedSessionProcessor(queueName, options, processor));
        return processor;
    }

    public override ServiceBusReceiver CreateReceiver(string queueName)
    {
        if (ReceiverFailure?.Invoke(queueName) is { } exception)
        {
            throw exception;
        }

        var receiver = new FakeServiceBusReceiver();
        CreatedReceivers.Add(receiver);
        return receiver;
    }

    public override ValueTask DisposeAsync() => default;
}

/// <summary>
/// Records a <see cref="ServiceBusProcessor"/> created through <see cref="FakeServiceBusClient"/>,
/// pairing it with the queue name and options it was created with.
/// </summary>
internal sealed record CreatedProcessor(string QueueName, ServiceBusProcessorOptions Options, FakeServiceBusProcessor Processor);

/// <summary>
/// Records a <see cref="ServiceBusSessionProcessor"/> created through <see cref="FakeServiceBusClient"/>,
/// pairing it with the queue name and options it was created with.
/// </summary>
internal sealed record CreatedSessionProcessor(
    string QueueName,
    ServiceBusSessionProcessorOptions Options,
    FakeServiceBusSessionProcessor Processor);

/// <summary>
/// A <see cref="ServiceBusProcessor"/> test double whose processing loop never runs; start and stop
/// complete immediately, and <see cref="RaiseProcessErrorAsync"/> lets tests invoke the handler
/// registered via <see cref="ServiceBusProcessor.ProcessErrorAsync"/> without a live namespace.
/// </summary>
internal sealed class FakeServiceBusProcessor : ServiceBusProcessor
{
    public Action? OnStopProcessing { get; set; }

    public int StartProcessingCallCount { get; private set; }

    public int StopProcessingCallCount { get; private set; }

    public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        StartProcessingCallCount++;
        return Task.CompletedTask;
    }

    public override Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        StopProcessingCallCount++;
        OnStopProcessing?.Invoke();
        return Task.CompletedTask;
    }

    public Task RaiseProcessErrorAsync(ProcessErrorEventArgs args) => OnProcessErrorAsync(args);
}

/// <summary>
/// A <see cref="ServiceBusSessionProcessor"/> test double whose processing loop never runs; start
/// and stop complete immediately, and <see cref="RaiseProcessErrorAsync"/> lets tests invoke the
/// handler registered via <see cref="ServiceBusSessionProcessor.ProcessErrorAsync"/> without a live
/// namespace.
/// </summary>
internal sealed class FakeServiceBusSessionProcessor : ServiceBusSessionProcessor
{
    // The SDK forwards ProcessMessageAsync/ProcessErrorAsync subscriptions to an inner
    // ServiceBusProcessor that the mocking-only parameterless constructor never assigns;
    // without one, subscribing throws a NullReferenceException.
    private static readonly FieldInfo s_innerProcessorField = typeof(ServiceBusSessionProcessor)
        .GetField("<InnerProcessor>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public FakeServiceBusSessionProcessor()
    {
        s_innerProcessorField.SetValue(this, new FakeInnerProcessor());
    }

    public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RaiseProcessErrorAsync(ProcessErrorEventArgs args) => OnProcessErrorAsync(args);

    private sealed class FakeInnerProcessor : ServiceBusProcessor
    {
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

/// <summary>
/// A <see cref="ServiceBusReceiver"/> test double used for the reply queue heartbeat; disposal
/// completes immediately without contacting a live namespace.
/// </summary>
internal sealed class FakeServiceBusReceiver : ServiceBusReceiver
{
    private bool _closed;

    public Action? OnDisposing { get; set; }

    public override bool IsClosed => _closed;

    public override ValueTask DisposeAsync()
    {
        OnDisposing?.Invoke();
        _closed = true;
        return default;
    }
}
