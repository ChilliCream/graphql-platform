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
    private TimeProvider _timeProvider = TimeProvider.System;
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
        _timeProvider = context.Services.GetTimeProvider();

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
        DrainTimeout = configuration.DrainTimeout ?? NatsTransportConfiguration.DefaultDrainTimeout;

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
    /// Gets how long a stopping transport lets in-flight handlers finish before cancelling them.
    /// </summary>
    public TimeSpan DrainTimeout { get; private set; } = NatsTransportConfiguration.DefaultDrainTimeout;

    /// <summary>
    /// Stops every receive endpoint pulling, with one drain deadline shared by all of them.
    /// </summary>
    /// <param name="cancellationToken">A token that ends the drain early.</param>
    protected override ValueTask OnBeforeStopAsync(CancellationToken cancellationToken)
    {
        var deadline = GetDrainDeadline();

        foreach (var endpoint in ReceiveEndpoints)
        {
            if (endpoint is NatsReceiveEndpoint natsEndpoint)
            {
                natsEndpoint.BeginStop(deadline);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets the point in time at which a drain that starts now cancels the handlers still running.
    /// </summary>
    internal DateTimeOffset GetDrainDeadline() => _timeProvider.GetUtcNow() + DrainTimeout;

    /// <summary>
    /// Gets the time remaining until <paramref name="deadline"/>, never less than zero.
    /// </summary>
    /// <param name="deadline">The drain deadline.</param>
    internal TimeSpan GetRemaining(DateTimeOffset deadline)
    {
        var remaining = deadline - _timeProvider.GetUtcNow();

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Provisions streams, binds each consumer to the stream capturing its subjects, and then
    /// provisions the consumers, before any endpoint starts.
    /// </summary>
    /// <param name="context">The configuration context for the current start-up phase.</param>
    /// <param name="cancellationToken">A token to cancel start-up.</param>
    // The order matters: a consumer can only be created once its stream exists.
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
    private async ValueTask EnsureConventionStreamAsync(CancellationToken cancellationToken)
    {
        // Under explicit binding the stream capturing the subjects is declared or already exists.
        if (BindMode is not MessagingBindMode.Implicit)
        {
            return;
        }

        var streamName = NatsNaming.ToStreamName(_streamName);
        var unclaimed = new List<string>();

        foreach (var subject in _topology.Subjects)
        {
            if (subject.IsCore
                || subject.StreamName is not null
                || unclaimed.Contains(subject.Subject, StringComparer.Ordinal))
            {
                continue;
            }

            if (_topology.FindStreamForSubject(subject.Subject) is not null)
            {
                continue;
            }

            // A subject this service's own stream captures stays listed: provisioning replaces the
            // stream's whole subject list.
            var owner = await NatsStreamResolver.FindCapturingStreamAsync(
                JetStream,
                subject.Subject,
                cancellationToken);

            if (owner is not null && !string.Equals(owner, streamName, StringComparison.Ordinal))
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
            // Wildcards are skipped, because the server rejects a token after '>'.
            unclaimed.AddRange([
                .. unclaimed
                    .Where(static subject => !SubjectMatcher.IsWildcard(subject))
                    .Select(NatsScheduling.ToSchedulingFilter)
            ]);
        }

        _topology.AddStream(new NatsStreamConfiguration
        {
            Name = streamName,

            // The server rejects a stream holding a wildcard together with subjects it covers.
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
                // Another service claimed these subjects between the check and the create.
                _logger.YieldedConventionStream(stream.Name, exception.Error.Description);

                _topology.RemoveStream(stream);
            }
            catch (NatsJSApiException exception) when (IsSubjectOverlap(exception))
            {
                throw ThrowHelper.StreamSubjectsOverlap(
                    stream.Name,
                    await DescribeSubjectConflictAsync(stream, exception, cancellationToken),
                    exception);
            }
        }
    }

    private static bool IsSubjectOverlap(NatsJSApiException exception)
        => exception.Error.ErrCode == SubjectOverlapErrorCode;

    /// <summary>
    /// Describes which of a declared stream's subjects are already owned elsewhere, and by which
    /// stream.
    /// </summary>
    /// <param name="stream">The stream that could not be provisioned.</param>
    /// <param name="exception">The overlap error the server returned.</param>
    /// <param name="cancellationToken">A token to cancel the lookup.</param>
    /// <returns>The description of the conflicting subjects.</returns>
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

        return conflicts.Count > 0
            ? string.Join("; ", conflicts)
            : exception.Error.Description ?? "the server reported overlapping subjects";
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

    /// <summary>
    /// Gets a value indicating whether the transport creates and removes <paramref name="resource"/>.
    /// </summary>
    /// <param name="resource">The stream or consumer.</param>
    internal bool ShouldProvision(INatsResource resource)
        => resource.AutoProvision ?? _topology.AutoProvision;

    private void AssertStreamSupported(NatsStream stream)
    {
        if (stream.AllowMsgTtl && !Capabilities.SupportsMessageTtl)
        {
            throw ThrowHelper.StreamRequiresServerVersion(
                stream.Name,
                "per-message TTL",
                "2.11",
                Capabilities.Version);
        }

        if (stream.AllowMsgSchedules && !Capabilities.SupportsMessageSchedules)
        {
            throw ThrowHelper.StreamRequiresServerVersion(
                stream.Name,
                "message schedules",
                "2.12",
                Capabilities.Version);
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

                        // Zero means the server's default window applies.
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
