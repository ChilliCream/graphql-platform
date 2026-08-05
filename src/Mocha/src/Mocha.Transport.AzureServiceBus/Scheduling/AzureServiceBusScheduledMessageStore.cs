using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
internal sealed class AzureServiceBusScheduledMessageStore : IScheduledMessageStore
{
    internal const string TokenPrefix = "asb:";
    private const string TokenVersion = "v1";

    private readonly AzureServiceBusMessagingTransport _transport;
    private readonly string _owner;

    public AzureServiceBusScheduledMessageStore(AzureServiceBusMessagingTransport transport)
    {
        _transport = transport;
        _owner = CreateOwner(transport);
    }

    /// <inheritdoc />
    public async ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
    {
        if (context.Endpoint is not AzureServiceBusDispatchEndpoint endpoint)
        {
            throw ThrowHelper.ScheduledMessageStoreRequiresAsbEndpoint(
                context.Endpoint?.GetType().Name ?? "null");
        }

        if (!ReferenceEquals(context.Transport, _transport)
            || !ReferenceEquals(endpoint.Transport, _transport))
        {
            throw ThrowHelper.ScheduledMessageStoreRequiresMatchingTransport();
        }

        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.ScheduledMessageStoreRequiresEnvelope();
        }

        if (envelope.ScheduledTime is not { } scheduledTime)
        {
            throw ThrowHelper.ScheduledMessageStoreRequiresScheduledTime();
        }

        await endpoint.EnsureProvisionedAsync(cancellationToken);

        var entityPath = AzureServiceBusEntityPathResolver.Resolve(endpoint, envelope);
        var now = DateTimeOffset.UtcNow;
        var expectedEnqueueTime = scheduledTime > now ? scheduledTime : now;
        var message = AzureServiceBusMessageFactory.Create(envelope, expectedEnqueueTime);

        var sequenceNumber = await AzureServiceBusEntityNotFoundRetry.ExecuteAsync(
            _transport.ClientManager,
            endpoint,
            entityPath,
            (sender, ct) => sender.ScheduleMessageAsync(message, scheduledTime, ct),
            cancellationToken);

        return CreateToken(_owner, entityPath, sequenceNumber);
    }

    /// <inheritdoc />
    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        if (!TryParseToken(token, _owner, out var entityPath, out var sequenceNumber))
        {
            return false;
        }

        using var senderLease = _transport.ClientManager.AcquireSender(entityPath);

        try
        {
            await senderLease.Sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
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

    internal static string CreateToken(
        string owner,
        string entityPath,
        long sequenceNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(owner);
        ArgumentException.ThrowIfNullOrEmpty(entityPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber);

        var encodedEntityPath = Encode(entityPath);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{TokenPrefix}{TokenVersion}:{owner}:{encodedEntityPath}:{sequenceNumber}");
    }

    internal static bool TryParseToken(
        string token,
        string expectedOwner,
        out string entityPath,
        out long sequenceNumber)
    {
        entityPath = string.Empty;
        sequenceNumber = 0;

        if (string.IsNullOrEmpty(token)
            || string.IsNullOrEmpty(expectedOwner)
            || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var body = token.AsSpan(TokenPrefix.Length);
        Span<Range> ranges = stackalloc Range[5];
        var segmentCount = body.Split(ranges, ':');

        if (segmentCount != 4
            || !body[ranges[0]].SequenceEqual(TokenVersion)
            || !body[ranges[1]].SequenceEqual(expectedOwner))
        {
            return false;
        }

        if (!long.TryParse(
                body[ranges[3]],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out sequenceNumber)
            || sequenceNumber <= 0)
        {
            sequenceNumber = 0;
            return false;
        }

        return TryDecode(body[ranges[2]], out entityPath);
    }

    internal static string CreateOwner(string transportName, string fullyQualifiedNamespace)
    {
        ArgumentException.ThrowIfNullOrEmpty(transportName);
        ArgumentException.ThrowIfNullOrEmpty(fullyQualifiedNamespace);

        var value = string.Concat(
            transportName,
            "\n",
            fullyQualifiedNamespace.TrimEnd('.').ToLowerInvariant());
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string CreateOwner(AzureServiceBusMessagingTransport transport)
    {
        var configuration = (AzureServiceBusTransportConfiguration)transport.Configuration;
        var fullyQualifiedNamespace = configuration.FullyQualifiedNamespace;

        if (fullyQualifiedNamespace is null && configuration.ConnectionString is { } connectionString)
        {
            fullyQualifiedNamespace =
                ServiceBusConnectionStringProperties.Parse(connectionString).FullyQualifiedNamespace;
        }

        fullyQualifiedNamespace ??= transport.Topology.Address.Host;

        return CreateOwner(transport.Name, fullyQualifiedNamespace);
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryDecode(ReadOnlySpan<char> encoded, out string value)
    {
        value = string.Empty;
        if (encoded.IsEmpty)
        {
            return false;
        }

        var base64 = encoded.ToString()
            .Replace('-', '+')
            .Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64
        };

        try
        {
            value = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            return value.Length > 0 && Encode(value).AsSpan().SequenceEqual(encoded);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
