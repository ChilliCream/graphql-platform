using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats;

/// <summary>
/// NATS JetStream implementation of <see cref="MessagingTransport"/> that manages the connection,
/// stream and consumer provisioning, and the lifecycle of receive and dispatch endpoints.
/// </summary>
public sealed class NatsMessagingTransport : MessagingTransport
{
    /// <summary>
    /// JetStream's <c>err_code</c> for a stream whose subjects overlap an existing stream's.
    /// </summary>
    private const int SubjectOverlapErrorCode = 10065;

    private readonly Action<INatsMessagingTransportDescriptor> _configure;
    private NatsMessagingTopology _topology = null!;
    private ILogger _logger = null!;
    private string _streamName = NatsTransportConfiguration.DefaultName;

    /// <summary>
    /// Creates a new NATS transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor.</param>
    public NatsMessagingTransport(Action<INatsMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the JetStream context used to provision topology and publish messages.
    /// </summary>
    public INatsJSContext JetStream { get; private set; } = null!;

    /// <summary>
    /// Gets the provider supplying the connection this transport uses.
    /// </summary>
    public INatsConnectionProvider Connection { get; private set; } = null!;

    /// <inheritdoc />
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (NatsTransportConfiguration)Configuration;
        var services = context.Services.GetApplicationServices();

        _logger = services.GetRequiredService<ILogger<NatsMessagingTransport>>();

        Connection =
            configuration.ConnectionProvider?.Invoke(context.Services)
            ?? new NatsConnectionProvider(services.GetRequiredService<INatsConnection>());

        JetStream = new NatsJSContext(Connection.Connection);

        _streamName =
            configuration.StreamName
            ?? context.Host?.EffectiveServiceName
            ?? NatsTransportConfiguration.DefaultName;

        SchedulingEnabled = configuration.EnableScheduling;
        PublishDeduplicationEnabled = configuration.EnablePublishDeduplication;

        WarnOnLossySubscriptionDefaults();

        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = Connection.Host,
            Port = Connection.Port
        };

        _topology = new NatsMessagingTopology(
            this,
            builder.Uri,
            configuration.Defaults,
            configuration.AutoProvision ?? true);

        foreach (var stream in configuration.Streams)
        {
            _topology.AddStream(stream);
        }

        foreach (var consumer in configuration.Consumers)
        {
            _topology.AddConsumer(consumer);
        }
    }

    private void WarnOnLossySubscriptionDefaults()
    {
        if (Connection.Connection.Opts.SubPendingChannelFullMode == BoundedChannelFullMode.Wait)
        {
            return;
        }

        _logger.LossySubscriptionDefaults(
            Connection.Connection.Opts.SubPendingChannelFullMode.ToString(),
            Connection.Connection.Opts.SubPendingChannelCapacity);
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (TryGetReplyDispatchEndpoint(address, out endpoint))
        {
            return true;
        }

        foreach (var candidate in DispatchEndpoints)
        {
            if (candidate.IsCompleted && candidate.Address == address)
            {
                endpoint = candidate;
                return true;
            }
        }

        if (_topology?.Address.IsBaseOf(address) == true)
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.IsCompleted && candidate.Destination?.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (NatsDestinations.TryResolveExplicit(Schema, address, out var subject))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.IsCompleted
                    && candidate is NatsDispatchEndpoint { Subject.Subject: { } candidateSubject }
                    && candidateSubject == subject)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        endpoint = null;
        return false;
    }

    /// <summary>
    /// Gets the version-gated JetStream features the connected server supports.
    /// </summary>
    public NatsServerCapabilities Capabilities { get; private set; } = NatsServerCapabilities.FromServerVersion(null);

    /// <summary>
    /// Gets a value indicating whether the transport was configured for scheduled and expiring
    /// messages.
    /// </summary>
    public bool SchedulingEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a publish carries the header JetStream deduplicates on.
    /// </summary>
    public bool PublishDeduplicationEnabled { get; private set; }

    /// <summary>
    /// Provisions streams, binds each consumer to the stream capturing its subjects, and then
    /// provisions the consumers, before any endpoint starts.
    /// </summary>
    /// <param name="context">The configuration context for the current start-up phase.</param>
    /// <param name="cancellationToken">A token to cancel start-up.</param>
    // The ordering is load-bearing. A JetStream publish to a subject no stream captures fails with
    // no-responders rather than silently succeeding the way RabbitMQ does, and consumers can only be
    // created once their stream exists.
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        Capabilities = NatsServerCapabilities.FromServerVersion(Connection.Connection.ServerInfo?.Version);

        await EnsureConventionStreamAsync(cancellationToken);

        WarnOnUnwieldyNames();

        await ProvisionStreamsAsync(cancellationToken);

        await NatsStreamResolver.ResolveAsync(JetStream, _topology, cancellationToken);

        await NatsStreamResolver.VerifySubjectsAsync(JetStream, _topology, cancellationToken);

        await ProvisionConsumersAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a stream for the subjects this service publishes that nothing else already captures.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    // Stream subjects must be disjoint, so a service claims only what nothing else has claimed and
    // binds to the owning stream for the rest. Deferred to start-up because whether a subject is
    // already captured is a question only the server can answer.
    private async ValueTask EnsureConventionStreamAsync(CancellationToken cancellationToken)
    {
        // Under explicit binding the caller owns the topology, so nothing is claimed on their behalf.
        // Subjects are still registered, because the endpoints resolve their destinations from them,
        // but the stream capturing them has to be declared or already exist on the server.
        if (BindMode is not MessagingBindMode.Implicit)
        {
            return;
        }

        var unclaimed = new List<string>();

        foreach (var subject in _topology.Subjects)
        {
            if (subject.IsCore
                || subject.StreamName is not null
                || unclaimed.Contains(subject.Subject, StringComparer.Ordinal))
            {
                continue;
            }

            // A stream declared on this bus is authoritative, so no round trip is needed for it.
            if (_topology.FindStreamForSubject(subject.Subject) is not null)
            {
                continue;
            }

            if (await NatsStreamResolver.IsCapturedAsync(JetStream, subject.Subject, cancellationToken))
            {
                continue;
            }

            unclaimed.Add(subject.Subject);
        }

        if (unclaimed.Count == 0)
        {
            return;
        }

        if (SchedulingEnabled)
        {
            // A filter, since each scheduled message gets its own subject in the namespace. Wildcards
            // are skipped: a token after '>' is rejected, and the filters covering them already apply.
            unclaimed.AddRange([
                .. unclaimed
                    .Where(static subject => !SubjectMatcher.IsWildcard(subject))
                    .Select(NatsScheduling.ToSchedulingFilter)
            ]);
        }

        _topology.AddStream(new NatsStreamConfiguration
        {
            Name = NatsNaming.ToStreamName(_streamName),

            // Collapsed because an endpoint may filter a wildcard that covers subjects derived from
            // message types, and the server rejects a stream holding both.
            Subjects = SubjectMatcher.Collapse(unclaimed),
            AllowMsgTtl = SchedulingEnabled,
            AllowMsgSchedules = SchedulingEnabled,
            Origin = TopologyOrigin.Convention
        });
    }

    /// <summary>
    /// Reports stream and consumer names long enough to make the server's storage directory names
    /// unwieldy.
    /// </summary>
    private void WarnOnUnwieldyNames()
    {
        foreach (var stream in _topology.Streams)
        {
            if (stream.Name.Length > NatsNaming.RecommendedMaxNameLength)
            {
                _logger.UnwieldyName("stream", stream.Name, NatsNaming.RecommendedMaxNameLength);
            }
        }

        foreach (var consumer in _topology.Consumers)
        {
            if (consumer.Name.Length > NatsNaming.RecommendedMaxNameLength)
            {
                _logger.UnwieldyName("consumer", consumer.Name, NatsNaming.RecommendedMaxNameLength);
            }
        }
    }

    private async ValueTask ProvisionStreamsAsync(CancellationToken cancellationToken)
    {
        foreach (var stream in _topology.Streams.ToList())
        {
            if (!ShouldProvision(stream))
            {
                continue;
            }

            AssertStreamSupported(stream);

            try
            {
                await stream.ProvisionAsync(JetStream, cancellationToken);
            }
            catch (NatsJSApiException exception)
                when (stream.Origin is TopologyOrigin.Convention && IsSubjectOverlap(exception))
            {
                // Another service claimed these subjects between the check and the create, so they
                // resolve to the stream that won. Convention streams only: discarding a declared one
                // would defer a configuration error to a much later publish failure.
                _logger.YieldedConventionStream(stream.Name, exception.Error.Description);

                _topology.RemoveStream(stream);
            }
            catch (NatsJSApiException exception) when (IsSubjectOverlap(exception))
            {
                throw new InvalidOperationException(
                    await DescribeSubjectConflictAsync(stream, exception, cancellationToken),
                    exception);
            }
        }
    }

    private static bool IsSubjectOverlap(NatsJSApiException exception)
        => exception.Error.ErrCode == SubjectOverlapErrorCode;

    /// <summary>
    /// Builds a message naming which of a declared stream's subjects are already owned elsewhere, and
    /// by which stream.
    /// </summary>
    /// <param name="stream">The stream that could not be provisioned.</param>
    /// <param name="exception">The overlap error the server returned.</param>
    /// <param name="cancellationToken">A token to cancel the lookup.</param>
    /// <returns>The message.</returns>
    private async ValueTask<string> DescribeSubjectConflictAsync(
        NatsStream stream,
        NatsJSApiException exception,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<string>();

        foreach (var subject in stream.Subjects)
        {
            await foreach (var owner in JetStream.ListStreamNamesAsync(subject, cancellationToken))
            {
                if (!string.Equals(owner, stream.Name, StringComparison.Ordinal))
                {
                    conflicts.Add($"'{subject}' is already captured by stream '{owner}'");
                }
            }
        }

        var detail = conflicts.Count > 0
            ? string.Join("; ", conflicts)
            : exception.Error.Description ?? "the server reported overlapping subjects";

        return $"Stream '{stream.Name}' cannot be provisioned because its subjects overlap a stream "
            + $"that already exists: {detail}. JetStream requires every stream's subjects to be "
            + "disjoint. Either remove the overlapping subject from this declaration, delete the "
            + "stream that owns it, or drop the declaration and let the transport bind to the "
            + "existing stream.";
    }

    private async ValueTask ProvisionConsumersAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _topology.Consumers)
        {
            if (ShouldProvision(consumer))
            {
                await consumer.ProvisionAsync(JetStream, cancellationToken);
            }
        }
    }

    private bool ShouldProvision(INatsResource resource)
        => resource.AutoProvision ?? _topology.AutoProvision;

    private void AssertStreamSupported(NatsStream stream)
    {
        if (stream.AllowMsgTtl && !Capabilities.SupportsMessageTtl)
        {
            throw new InvalidOperationException(
                $"Stream '{stream.Name}' enables per-message TTL, which requires NATS server 2.11 "
                + $"or later, but the server reports {Capabilities.Version}.");
        }

        if (stream.AllowMsgSchedules && !Capabilities.SupportsMessageSchedules)
        {
            throw new InvalidOperationException(
                $"Stream '{stream.Name}' enables message schedules, which requires NATS server 2.12 "
                + $"or later, but the server reports {Capabilities.Version}.");
        }
    }

    /// <inheritdoc />
    public override TransportDescription Describe()
    {
        var entities = new List<TopologyEntityDescription>();
        var links = new List<TopologyLinkDescription>();

        foreach (var stream in _topology.Streams)
        {
            entities.Add(
                new TopologyEntityDescription(
                    MochaUrn.TopologyEntity(stream.Address?.ToString(), "stream", stream.Name),
                    "stream",
                    stream.Name,
                    stream.Address?.ToString(),
                    "inbound",
                    new Dictionary<string, object?>
                    {
                        ["subjects"] = string.Join(", ", stream.Subjects),

                        // Zero means the server's default applies rather than deduplication being
                        // off, so reporting the number would misdescribe the stream.
                        ["duplicateWindow"] = stream.DuplicateWindow == TimeSpan.Zero
                            ? "server default"
                            : stream.DuplicateWindow.ToString(),
                        ["allowMsgTtl"] = stream.AllowMsgTtl,
                        ["allowMsgSchedules"] = stream.AllowMsgSchedules,
                        ["autoProvision"] = stream.AutoProvision ?? _topology.AutoProvision,
                        ["origin"] = stream.Origin
                    }));
        }

        foreach (var subject in _topology.Subjects)
        {
            entities.Add(
                new TopologyEntityDescription(
                    MochaUrn.TopologyEntity(subject.Address?.ToString(), "subject", subject.Subject),
                    "subject",
                    subject.Subject,
                    subject.Address?.ToString(),
                    "inbound",
                    new Dictionary<string, object?>
                    {
                        ["stream"] = subject.StreamName,
                        ["core"] = subject.IsCore,
                        ["origin"] = subject.Origin
                    }));
        }

        foreach (var consumer in _topology.Consumers)
        {
            entities.Add(
                new TopologyEntityDescription(
                    MochaUrn.TopologyEntity(consumer.Address?.ToString(), "consumer", consumer.Name),
                    "consumer",
                    consumer.Name,
                    consumer.Address?.ToString(),
                    "outbound",
                    new Dictionary<string, object?>
                    {
                        ["stream"] = consumer.StreamName,
                        ["filterSubjects"] = string.Join(", ", consumer.FilterSubjects),
                        ["maxAckPending"] = consumer.MaxAckPending,
                        ["ackProgressInterval"] = consumer.AckProgressInterval,
                        ["autoProvision"] = consumer.AutoProvision ?? _topology.AutoProvision,
                        ["origin"] = consumer.Origin
                    }));

            if (consumer.StreamName is not { } streamName)
            {
                continue;
            }

            var stream = _topology.Streams.FirstOrDefault(s => s.Name == streamName);

            links.Add(
                new TopologyLinkDescription(
                    MochaUrn.TopologyLink(null, "binding", stream?.Address?.ToString(), consumer.Address?.ToString()),
                    "binding",
                    consumer.Address?.ToString(),
                    stream?.Address?.ToString(),
                    consumer.Address?.ToString(),
                    "forward",
                    new Dictionary<string, object?>
                    {
                        ["filterSubjects"] = string.Join(", ", consumer.FilterSubjects)
                    }));
        }

        return new TransportDescription(
            Urn,
            _topology.Address.ToString(),
            Name,
            Schema,
            GetType().Name,
            [.. ReceiveEndpoints.Select(e => e.Describe())],
            [.. DispatchEndpoints.Select(e => e.Describe())],
            new TopologyDescription(_topology.Address.ToString(), entities, links));
    }

    /// <inheritdoc />
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new NatsMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <inheritdoc />
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new NatsReceiveEndpoint(this);
    }

    /// <inheritdoc />
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new NatsDispatchEndpoint(this);
    }
}
