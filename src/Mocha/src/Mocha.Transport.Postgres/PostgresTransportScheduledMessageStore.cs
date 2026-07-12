using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.Postgres;

internal sealed class PostgresTransportScheduledMessageStore(PostgresMessagingTransport transport)
    : IScheduledMessageStore
{
    internal const string TokenPrefix = "postgres-transport:";

    public async ValueTask<string> PersistAsync(
        IDispatchContext context,
        CancellationToken cancellationToken)
    {
        if (context.Transport is not PostgresMessagingTransport)
        {
            throw new InvalidOperationException(
                "Postgres transport scheduled message store requires a Postgres transport.");
        }

        if (context.Endpoint is not PostgresDispatchEndpoint endpoint)
        {
            throw new InvalidOperationException(
                "Postgres transport scheduled message store requires a Postgres dispatch endpoint.");
        }

        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        if (envelope.ScheduledTime is not { } scheduledTime)
        {
            throw new InvalidOperationException("Scheduled time is not set on the envelope.");
        }

        var feature = context.Features.GetOrSet<JsonHeadersFeature>();
        var headers = PostgresMessageHeadersWriter.Write(feature, envelope);
        var target = PostgresDispatchTargetResolver.Resolve(endpoint, envelope);

        if (target.IsTopic)
        {
            var ids = await transport.MessageStore.PublishScheduledAsync(
                envelope.Body,
                headers,
                target.Name,
                scheduledTime,
                cancellationToken);

            if (ids.Count == 0)
            {
                return "";
            }

            return TokenPrefix + string.Join(',', ids);
        }

        var id = await transport.MessageStore.SendScheduledAsync(
            envelope.Body,
            headers,
            target.Name,
            scheduledTime,
            cancellationToken);

        return TokenPrefix + id;
    }

    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        if (!TryParseToken(token, out var ids))
        {
            return false;
        }

        return await transport.MessageStore.CancelScheduledMessagesAsync(ids, cancellationToken);
    }

    private static bool TryParseToken(string token, out Guid[] ids)
    {
        ids = [];

        if (string.IsNullOrEmpty(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var value = token[TokenPrefix.Length..];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        ids = new Guid[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!Guid.TryParse(parts[i], out ids[i]))
            {
                ids = [];
                return false;
            }
        }

        return true;
    }
}
