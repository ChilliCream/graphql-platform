using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

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
