using Mocha.Middlewares;
using Mocha.Scheduling;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Hands scheduled messages to JetStream rather than storing them.
/// </summary>
// Registered because the dispatch pipeline refuses a scheduled send for a transport without a store.
internal sealed class NatsScheduledMessageStore(NatsMessagingTransport transport) : IScheduledMessageStore
{
    /// <summary>
    /// The prefix identifying cancellation tokens issued by this store.
    /// </summary>
    internal const string TokenPrefix = "nats-transport:";

    /// <summary>
    /// JetStream's <c>err_code</c> for a stream that does not exist.
    /// </summary>
    private const int StreamNotFoundErrorCode = 10059;

    /// <inheritdoc />
    public async ValueTask<string> PersistAsync(
        IDispatchContext context,
        CancellationToken cancellationToken)
    {
        if (context.Endpoint is not NatsDispatchEndpoint endpoint)
        {
            throw ThrowHelper.ScheduledStoreRequiresNatsEndpoint();
        }

        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        if (envelope.ScheduledTime is null)
        {
            throw ThrowHelper.ScheduledTimeNotSet();
        }

        // The scheduling subject holds only this message, so it is the cancellation handle.
        var subject = await endpoint.DispatchScheduledAsync(context, cancellationToken);

        return TokenPrefix + subject;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Removes the message still waiting on its scheduling subject. Returns <see langword="false"/>
    /// once it has been released to its target.
    /// </remarks>
    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var subject = token[TokenPrefix.Length..];

        if (subject.Length == 0 || SubjectMatcher.IsWildcard(subject))
        {
            return false;
        }

        var streamName = await ResolveStreamAsync(subject, cancellationToken);

        if (streamName is null)
        {
            return false;
        }

        try
        {
            var stream = await transport.JetStream.GetStreamAsync(
                streamName,
                cancellationToken: cancellationToken);

            var response = await stream.PurgeAsync(
                new StreamPurgeRequest { Filter = subject },
                cancellationToken);

            return response.Success && response.Purged > 0;
        }
        catch (NatsJSApiException exception) when (exception.Error.ErrCode == StreamNotFoundErrorCode)
        {
            return false;
        }
    }

    // Stream subjects are disjoint, so a concrete subject has at most one match.
    private async ValueTask<string?> ResolveStreamAsync(
        string subject,
        CancellationToken cancellationToken)
    {
        await foreach (var name in transport.JetStream.ListStreamNamesAsync(subject, cancellationToken))
        {
            return name;
        }

        return null;
    }
}
