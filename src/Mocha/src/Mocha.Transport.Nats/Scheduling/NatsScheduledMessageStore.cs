using Mocha.Middlewares;
using Mocha.Scheduling;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Hands scheduled messages to JetStream rather than storing them.
/// </summary>
// JetStream holds the message itself, so this publishes straight away with the schedule headers and
// lets the server release it when due. A store is still registered because the dispatch pipeline
// refuses a scheduled send for a transport without one.
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
            throw new InvalidOperationException(
                "The NATS scheduled message store requires a NATS dispatch endpoint.");
        }

        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set.");
        }

        if (envelope.ScheduledTime is null)
        {
            throw new InvalidOperationException("Scheduled time is not set on the envelope.");
        }

        // The subject is the handle: it holds exactly this one scheduled message, so removing it
        // cancels that message and nothing else.
        var subject = await endpoint.DispatchScheduledAsync(context, cancellationToken);

        return TokenPrefix + subject;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Removes the message still waiting on its scheduling subject. Returns <see langword="false"/>
    /// once it has been released to its target, because at that point there is no schedule left to
    /// withdraw.
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

    // The first match is the only match: the server keeps stream subjects disjoint, so a concrete
    // subject belongs to exactly one stream. Wildcards are rejected before reaching here.
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
