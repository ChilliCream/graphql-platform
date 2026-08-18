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

    public override ServiceBusSender CreateSender(string queueOrTopicName)
    {
        var senderIndex = _senderCount++;
        var sender = new FakeServiceBusSender(() => _failureAt(senderIndex));
        CreatedSenders.Add(sender);
        return sender;
    }

    public override ValueTask DisposeAsync() => default;
}

/// <summary>
/// A <see cref="ServiceBusSender"/> test double that counts send and schedule calls and optionally
/// throws a caller-supplied exception on each call.
/// </summary>
internal sealed class FakeServiceBusSender : ServiceBusSender
{
    private readonly Func<Exception?> _failureFactory;
    private bool _closed;

    public FakeServiceBusSender(Func<Exception?> failureFactory)
    {
        _failureFactory = failureFactory;
    }

    public int SendMessageCallCount { get; private set; }

    public int ScheduleMessageCallCount { get; private set; }

    public override bool IsClosed => _closed;

    public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        SendMessageCallCount++;

        if (_failureFactory() is { } exception)
        {
            throw exception;
        }

        return Task.CompletedTask;
    }

    public override Task<long> ScheduleMessageAsync(
        ServiceBusMessage message,
        DateTimeOffset scheduledEnqueueTime,
        CancellationToken cancellationToken = default)
    {
        ScheduleMessageCallCount++;

        if (_failureFactory() is { } exception)
        {
            throw exception;
        }

        return Task.FromResult(1L);
    }

    public override ValueTask DisposeAsync()
    {
        _closed = true;
        return default;
    }
}
