using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus.Features;

/// <summary>
/// Adapter over <see cref="ProcessMessageEventArgs"/> exposing the
/// <see cref="IAzureServiceBusMessageActions"/> settlement contract for non-session endpoints.
/// </summary>
internal readonly struct AzureServiceBusMessageActions(ProcessMessageEventArgs args)
    : IAzureServiceBusMessageActions
{
    public Task CompleteAsync(CancellationToken cancellationToken = default)
        => args.CompleteMessageAsync(args.Message, cancellationToken);

    public Task AbandonAsync(
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default)
        => args.AbandonMessageAsync(args.Message, propertiesToModify, cancellationToken);

    public Task DeadLetterAsync(
        string deadLetterReason,
        string? deadLetterErrorDescription = null,
        CancellationToken cancellationToken = default)
        => args.DeadLetterMessageAsync(
            args.Message,
            deadLetterReason,
            deadLetterErrorDescription,
            cancellationToken);
}
