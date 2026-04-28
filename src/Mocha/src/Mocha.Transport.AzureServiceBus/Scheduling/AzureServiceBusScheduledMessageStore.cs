using System.Globalization;
using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.AzureServiceBus.Scheduling;

/// <summary>
/// Implements <see cref="IScheduledMessageStore"/> for Azure Service Bus by scheduling messages
/// through <see cref="ServiceBusSender.ScheduleMessageAsync(ServiceBusMessage, DateTimeOffset, CancellationToken)"/>
/// and cancelling them through
/// <see cref="ServiceBusSender.CancelScheduledMessageAsync(long, CancellationToken)"/>.
/// </summary>
internal sealed class AzureServiceBusScheduledMessageStore(AzureServiceBusClientManager clientManager)
    : IScheduledMessageStore
{
    private const string TokenPrefix = "asb:";

    /// <inheritdoc />
    public async ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
    {
        if (context.Endpoint is not AzureServiceBusDispatchEndpoint endpoint)
        {
            throw new InvalidOperationException(
                "AzureServiceBusScheduledMessageStore requires an AzureServiceBusDispatchEndpoint, "
                + $"but the dispatch context carries a '{context.Endpoint.GetType().Name}'.");
        }

        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException(
                "AzureServiceBusScheduledMessageStore requires a serialized envelope on the dispatch context.");
        }

        if (envelope.ScheduledTime is not { } scheduledTime)
        {
            throw new InvalidOperationException(
                "AzureServiceBusScheduledMessageStore requires the envelope to carry a scheduled time.");
        }

        await endpoint.EnsureProvisionedAsync(cancellationToken);

        var entityPath = AzureServiceBusEntityPathResolver.Resolve(endpoint, envelope);
        var message = AzureServiceBusMessageFactory.Create(envelope);

        var sequenceNumber = await AzureServiceBusEntityNotFoundRetry.ExecuteAsync(
            clientManager,
            endpoint,
            entityPath,
            (sender, ct) => sender.ScheduleMessageAsync(message, scheduledTime, ct),
            cancellationToken);

        return $"{TokenPrefix}{entityPath}:{sequenceNumber.ToString(CultureInfo.InvariantCulture)}";
    }

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
        catch (ServiceBusException ex)
            when (ex.Reason == ServiceBusFailureReason.MessageNotFound
                || ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound
            )
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
