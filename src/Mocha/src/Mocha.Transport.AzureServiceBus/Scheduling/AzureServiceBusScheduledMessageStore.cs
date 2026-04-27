using System.Globalization;
using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.AzureServiceBus.Scheduling;

/// <summary>
/// Implements <see cref="IScheduledMessageStore"/> for Azure Service Bus by delegating cancellation
/// to <see cref="ServiceBusSender.CancelScheduledMessageAsync(long, CancellationToken)"/> using the
/// sequence number embedded in the opaque token.
/// </summary>
/// <remarks>
/// The Azure Service Bus transport sets <see cref="SchedulingTransportFeature.SupportsSchedulingNatively"/>
/// to <c>true</c>, so the dispatch scheduling middleware never invokes <see cref="PersistAsync"/> on this
/// store. Persistence is handled directly in the dispatch endpoint via
/// <see cref="ServiceBusSender.ScheduleMessageAsync(ServiceBusMessage, DateTimeOffset, CancellationToken)"/>.
/// </remarks>
internal sealed class AzureServiceBusScheduledMessageStore(AzureServiceBusClientManager clientManager)
    : IScheduledMessageStore
{
    private const string TokenPrefix = "asb:";

    /// <inheritdoc />
    /// <remarks>
    /// This method is unreachable when the Azure Service Bus transport is in use because
    /// <see cref="SchedulingTransportFeature.SupportsSchedulingNatively"/> is set to <c>true</c>,
    /// causing the dispatch scheduling middleware to be skipped during pipeline construction.
    /// </remarks>
    public ValueTask<string> PersistAsync(
        MessageEnvelope envelope,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "AzureServiceBusScheduledMessageStore.PersistAsync is unreachable; "
            + "the Azure Service Bus transport schedules messages via ScheduleMessageAsync "
            + "in the dispatch endpoint (SupportsSchedulingNatively = true).");

    /// <inheritdoc />
    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        if (!TryParseToken(token, out var entityPath, out var sequenceNumber))
        {
            return false;
        }

        var sender = clientManager.GetSender(entityPath);

        try
        {
            await sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
            return true;
        }
        // MessageNotFound: scheduled message already cancelled or delivered.
        // MessagingEntityNotFound: the queue/topic itself is gone (e.g. AutoDeleteOnIdle fired),
        // so the scheduled message is gone with it. Either way, cancellation is vacuously satisfied.
        catch (ServiceBusException ex) when (
            ex.Reason == ServiceBusFailureReason.MessageNotFound
            || ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return false;
        }
    }

    private static bool TryParseToken(string token, out string entityPath, out long sequenceNumber)
    {
        entityPath = string.Empty;
        sequenceNumber = 0;

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        // "asb:{entityPath}:{sequenceNumber}"
        if (!token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var body = token.AsSpan(TokenPrefix.Length);
        var lastColon = body.LastIndexOf(':');
        if (lastColon <= 0 || lastColon == body.Length - 1)
        {
            return false;
        }

        entityPath = body[..lastColon].ToString();
        var seqSpan = body[(lastColon + 1)..];
        return long.TryParse(seqSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out sequenceNumber);
    }
}
