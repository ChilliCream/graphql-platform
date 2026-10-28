using System.Globalization;
using Mocha.Middlewares;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// NATS dispatch endpoint that publishes outbound messages to a JetStream subject.
/// </summary>
/// <param name="transport">The owning NATS transport instance.</param>
public sealed class NatsDispatchEndpoint(NatsMessagingTransport transport)
    : DispatchEndpoint<NatsDispatchEndpointConfiguration>(transport)
{
    private static readonly INatsSerialize<ReadOnlyMemory<byte>> s_serializer =
        NatsRawSerializer<ReadOnlyMemory<byte>>.Default;

    /// <summary>
    /// Gets the subject this endpoint publishes to.
    /// </summary>
    public NatsSubject Subject { get; private set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        NatsDispatchEndpointConfiguration configuration)
    {
        if (configuration.Subject is null)
        {
            throw ThrowHelper.SubjectRequired();
        }
    }

    /// <inheritdoc />
    protected override void OnComplete(
        IMessagingConfigurationContext context,
        NatsDispatchEndpointConfiguration configuration)
    {
        var topology = (NatsMessagingTopology)Transport.Topology;

        Subject =
            topology.Subjects.FirstOrDefault(s => s.Subject == configuration.Subject)
            ?? throw ThrowHelper.SubjectNotFound(configuration.Subject);

        Destination = Subject;

        // Not Subject.Address, which holds a placeholder stream until start-up binds the subject.
        Address = new Uri($"{Transport.Schema}:{NatsAddress.SubjectSegment}/{configuration.Subject}");
    }

    /// <inheritdoc />
    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var cancellationToken = context.CancellationToken;

        if (Kind is DispatchEndpointKind.Reply)
        {
            await PublishReplyAsync(envelope, headers, cancellationToken);
            return;
        }

        var subject = ApplyScheduling(envelope, headers, context.Services.GetTimeProvider().GetUtcNow());

        await PublishAsync(subject, envelope, headers, cancellationToken);
    }

    /// <summary>
    /// Publishes a scheduled message, letting JetStream hold it until it is due.
    /// </summary>
    /// <param name="context">The dispatch context.</param>
    /// <param name="cancellationToken">A token to cancel the publish.</param>
    internal async ValueTask<string> DispatchScheduledAsync(
        IDispatchContext context,
        CancellationToken cancellationToken)
    {
        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var subject = ApplyScheduling(envelope, headers, context.Services.GetTimeProvider().GetUtcNow());

        await PublishAsync(subject, envelope, headers, cancellationToken);

        return subject;
    }

    /// <summary>
    /// Applies scheduling and expiry headers, returning the subject to publish to.
    /// </summary>
    /// <param name="envelope">The envelope being dispatched.</param>
    /// <param name="headers">The headers being sent.</param>
    /// <param name="now">The current time, from the bus's time provider.</param>
    /// <returns>
    /// The scheduling subject when the message is scheduled, otherwise the endpoint's own subject.
    /// </returns>
    private string ApplyScheduling(MessageEnvelope envelope, NatsHeaders headers, DateTimeOffset now)
    {
        if (envelope.DeliverBy is { } deliverBy)
        {
            AssertSupported(
                transport.Capabilities.SupportsMessageTtl,
                "per-message TTL",
                "2.11");

            headers.Add(NatsScheduling.TtlHeader, NatsScheduling.ToTtlValue(deliverBy - now));
        }

        if (envelope.ScheduledTime is not { } scheduledTime || scheduledTime <= now)
        {
            return Subject.Subject;
        }

        AssertSupported(
            transport.Capabilities.SupportsMessageSchedules,
            "message schedules",
            "2.12");

        if (!transport.SchedulingEnabled)
        {
            throw ThrowHelper.SchedulingNotEnabled(scheduledTime, Subject.Subject);
        }

        headers.Add(NatsScheduling.ScheduleHeader, NatsScheduling.ToScheduleValue(scheduledTime));
        headers.Add(NatsScheduling.ScheduleTargetHeader, Subject.Subject);

        return NatsScheduling.ToSchedulingSubject(Subject.Subject, NatsScheduling.NewScheduleId());
    }

    private static void AssertSupported(bool supported, string feature, string minimumVersion)
    {
        if (!supported)
        {
            throw ThrowHelper.ServerVersionRequired(feature, minimumVersion);
        }
    }

    private async ValueTask PublishAsync(
        string subject,
        MessageEnvelope envelope,
        NatsHeaders headers,
        CancellationToken cancellationToken)
    {
        // Qualified by subject, because JetStream deduplicates per stream rather than per subject.
        if (transport.PublishDeduplicationEnabled && envelope.MessageId is { Length: > 0 } messageId)
        {
            headers[NatsMessageHeaders.DeduplicationKey] = $"{subject}:{messageId}";
        }

        PubAckResponse ack;

        try
        {
            ack = await transport.JetStream.PublishAsync(
                subject,
                envelope.Body,
                s_serializer,
                opts: null,
                headers,
                cancellationToken);
        }
        catch (NatsException exception)
        {
            throw ThrowHelper.PublishFailed(subject, exception);
        }

        // Not IsSuccess(), which also reports a duplicate as a failure.
        if (ack.Error is { } error)
        {
            throw ThrowHelper.PublishRejected(
                subject,
                error.Description ?? error.Code.ToString(CultureInfo.InvariantCulture));
        }
    }

    private async ValueTask PublishReplyAsync(
        MessageEnvelope envelope,
        NatsHeaders headers,
        CancellationToken cancellationToken)
    {
        var subject = ResolveReplySubject(envelope);

        await transport.Connection.Connection.PublishAsync(
            subject,
            envelope.Body,
            headers,
            replyTo: null,
            s_serializer,
            opts: null,
            cancellationToken);
    }

    private string ResolveReplySubject(MessageEnvelope envelope)
    {
        if (envelope.DestinationAddress is not { } destinationAddress)
        {
            throw ThrowHelper.ReplyDestinationNotSet();
        }

        if (Uri.TryCreate(destinationAddress, UriKind.Absolute, out var address)
            && NatsDestinations.TryResolveExplicit(transport.Schema, address, out var subject))
        {
            return subject;
        }

        return destinationAddress;
    }
}
