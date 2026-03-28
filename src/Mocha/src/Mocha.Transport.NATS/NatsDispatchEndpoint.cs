using Mocha.Middlewares;
using NATS.Client.Core;
using NATS.Client.JetStream;
using static System.StringSplitOptions;

namespace Mocha.Transport.NATS;

/// <summary>
/// NATS JetStream dispatch endpoint that publishes outbound messages to a target subject
/// using the transport's JetStream context.
/// </summary>
/// <param name="transport">The owning NATS transport instance.</param>
public sealed class NatsDispatchEndpoint(NatsMessagingTransport transport)
    : DispatchEndpoint<NatsDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target subject for this endpoint.
    /// </summary>
    public NatsSubject? Subject { get; private set; }

    private readonly SemaphoreSlim _provisionLock = new(1, 1);
    private bool _isProvisioned;

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var cancellationToken = context.CancellationToken;

        await EnsureProvisionedAsync(cancellationToken);

        var subject = ResolveSubject(envelope);
        var headers = BuildNatsHeaders(envelope);

        var ack = await transport.JetStream.PublishAsync<ReadOnlyMemory<byte>>(
            subject,
            envelope.Body,
            headers: headers,
            cancellationToken: cancellationToken);

        ack.EnsureSuccess();
    }

    private async ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _isProvisioned))
        {
            return;
        }

        await _provisionLock.WaitAsync(cancellationToken);

        try
        {
            if (_isProvisioned)
            {
                return;
            }

            var autoProvision = ((NatsMessagingTopology)transport.Topology).AutoProvision;
            var js = transport.JetStream;

            if (Subject is not null)
            {
                var stream = ((NatsMessagingTopology)transport.Topology).GetStreamForSubject(Subject.Name);
                if (stream is not null && (stream.AutoProvision ?? autoProvision))
                {
                    await stream.ProvisionAsync(js, cancellationToken);
                }
            }

            Volatile.Write(ref _isProvisioned, true);
        }
        finally
        {
            _provisionLock.Release();
        }
    }

    private string ResolveSubject(MessageEnvelope envelope)
    {
        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "s")
                {
                    return new string(name);
                }
            }

            throw new InvalidOperationException(
                $"Cannot determine subject from destination address {destinationAddress}");
        }

        if (Subject is null)
        {
            throw new InvalidOperationException("Subject is not set on this dispatch endpoint.");
        }

        return Subject.Name;
    }

    private static NatsHeaders? BuildNatsHeaders(MessageEnvelope envelope)
    {
        if (envelope.MessageId is null
            && envelope.CorrelationId is null
            && envelope.ConversationId is null
            && envelope.CausationId is null
            && envelope.SourceAddress is null
            && envelope.DestinationAddress is null
            && envelope.ResponseAddress is null
            && envelope.FaultAddress is null
            && envelope.ContentType is null
            && envelope.MessageType is null
            && envelope.SentAt is null
            && envelope.DeliverBy is null
            && envelope.EnclosedMessageTypes is not { Length: > 0 }
            && (envelope.Headers is null || envelope.Headers.Count == 0))
        {
            return null;
        }

        var headers = new NatsHeaders();

        if (envelope.MessageId is not null)
        {
            headers.Add(NatsMessageHeaders.MessageId, envelope.MessageId);
        }

        if (envelope.CorrelationId is not null)
        {
            headers.Add(NatsMessageHeaders.CorrelationId, envelope.CorrelationId);
        }

        if (envelope.ConversationId is not null)
        {
            headers.Add(NatsMessageHeaders.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            headers.Add(NatsMessageHeaders.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            headers.Add(NatsMessageHeaders.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            headers.Add(NatsMessageHeaders.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.ResponseAddress is not null)
        {
            headers.Add(NatsMessageHeaders.ResponseAddress, envelope.ResponseAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            headers.Add(NatsMessageHeaders.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.ContentType is not null)
        {
            headers.Add(NatsMessageHeaders.ContentType, envelope.ContentType);
        }

        if (envelope.MessageType is not null)
        {
            headers.Add(NatsMessageHeaders.MessageType, envelope.MessageType);
        }

        if (envelope.SentAt is not null)
        {
            headers.Add(NatsMessageHeaders.SentAt, envelope.SentAt.Value.ToString("O"));
        }

        if (envelope.DeliverBy is not null)
        {
            headers.Add(NatsMessageHeaders.DeliverBy, envelope.DeliverBy.Value.ToString("O"));
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 })
        {
            headers.Add(NatsMessageHeaders.EnclosedMessageTypes,
                string.Join(";", envelope.EnclosedMessageTypes.Value));
        }

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is not null)
                {
                    headers.Add(header.Key, header.Value.ToString()!);
                }
            }
        }

        return headers;
    }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        NatsDispatchEndpointConfiguration configuration)
    {
        if (configuration.SubjectName is null)
        {
            throw new InvalidOperationException("Subject name is required");
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        NatsDispatchEndpointConfiguration configuration)
    {
        var topology = (NatsMessagingTopology)Transport.Topology;
        if (configuration.SubjectName is not null)
        {
            Subject = topology.Subjects.FirstOrDefault(s => s.Name == configuration.SubjectName)
                ?? throw new InvalidOperationException(
                    $"Subject '{configuration.SubjectName}' not found in topology");
        }

        Destination = Subject as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}
