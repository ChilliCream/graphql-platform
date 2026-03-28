using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using static System.StringSplitOptions;

namespace Mocha.Transport.NATS;

/// <summary>
/// NATS JetStream implementation of <see cref="MessagingTransport"/> that manages connections, topology provisioning,
/// and the lifecycle of receive and dispatch endpoints backed by NATS JetStream streams and consumers.
/// </summary>
public sealed class NatsMessagingTransport : MessagingTransport
{
    private readonly Action<INatsMessagingTransportDescriptor> _configure;
    private NatsMessagingTopology _topology = null!;
    private NatsConnection _connection = null!;
    private INatsJSContext _jetStream = null!;
    private bool _ownsConnection;

    /// <summary>
    /// Creates a new NATS transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor with endpoints, topology, and connection settings.</param>
    public NatsMessagingTransport(Action<INatsMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the JetStream context used for publishing and consuming messages.
    /// </summary>
    public INatsJSContext JetStream => _jetStream;

    /// <summary>
    /// Gets the NATS connection used by this transport.
    /// </summary>
    public NatsConnection Connection => _connection;

    /// <summary>
    /// Resolves or creates the NATS connection, builds the transport topology URI, and creates
    /// the JetStream context used for the lifetime of this transport.
    /// </summary>
    /// <remarks>
    /// Called once during the messaging host initialization phase, after the base transport has
    /// been initialized. Connection ownership is tracked: only connections created by the transport
    /// are disposed when the transport is disposed. DI-resolved and factory-provided connections
    /// are not owned by the transport.
    /// </remarks>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (NatsTransportConfiguration)Configuration;

        if (configuration.ConnectionFactory is not null)
        {
            _connection = configuration.ConnectionFactory(context.Services);
            _ownsConnection = false;
        }
        else
        {
            var existing = context.Services.GetApplicationServices().GetService<NatsConnection>();
            if (existing is not null)
            {
                _connection = existing;
                _ownsConnection = false;
            }
            else
            {
                _connection = new NatsConnection(new NatsOpts
                {
                    Url = configuration.Url ?? "nats://localhost:4222",
                    Name = Name
                });
                _ownsConnection = true;
            }
        }

        _jetStream = _connection.CreateJetStreamContext();

        // Derive topology address from the actual connection URL, falling back to config URL
        var url = _connection.Opts.Url ?? configuration.Url ?? "nats://localhost:4222";
        var connectionUri = new Uri(url);
        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = connectionUri.Host,
            Port = connectionUri.Port,
            Path = "/"
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

    /// <summary>
    /// Produces a structural description of this NATS transport including its endpoints,
    /// topology streams, subjects, consumers, and their relationships.
    /// </summary>
    /// <returns>A <see cref="TransportDescription"/> capturing the current transport topology and endpoint state.</returns>
    public override TransportDescription Describe()
    {
        var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
        var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

        var entities = new List<TopologyEntityDescription>();
        var links = new List<TopologyLinkDescription>();
        var autoProvision = _topology.AutoProvision;

        foreach (var stream in _topology.Streams)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "stream",
                    stream.Name,
                    stream.Address?.ToString(),
                    "inbound",
                    new Dictionary<string, object?>
                    {
                        ["subjects"] = string.Join(", ", stream.Subjects),
                        ["maxMsgs"] = stream.MaxMsgs,
                        ["maxBytes"] = stream.MaxBytes,
                        ["maxAge"] = stream.MaxAge?.ToString(),
                        ["replicas"] = stream.Replicas,
                        ["autoProvision"] = stream.AutoProvision ?? autoProvision
                    }));
        }

        foreach (var consumer in _topology.Consumers)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "consumer",
                    consumer.Name,
                    consumer.Address?.ToString(),
                    "outbound",
                    new Dictionary<string, object?>
                    {
                        ["streamName"] = consumer.StreamName,
                        ["filterSubject"] = consumer.FilterSubject,
                        ["maxAckPending"] = consumer.MaxAckPending,
                        ["ackWait"] = consumer.AckWait?.ToString(),
                        ["maxDeliver"] = consumer.MaxDeliver,
                        ["autoProvision"] = consumer.AutoProvision ?? autoProvision
                    }));
        }

        foreach (var subject in _topology.Subjects)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "subject",
                    subject.Name,
                    subject.Address?.ToString(),
                    null,
                    new Dictionary<string, object?>
                    {
                        ["streamName"] = subject.StreamName
                    }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            _topology.Address.ToString(),
            Name,
            Schema,
            GetType().Name,
            receiveEndpoints,
            dispatchEndpoints,
            topology);
    }

    /// <summary>
    /// Ensures that the NATS connection is established before the transport's endpoints begin
    /// processing messages. Uses <c>PingAsync</c> to force connection establishment and verify
    /// the server is reachable.
    /// </summary>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the connection establishment.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        await _connection.PingAsync(cancellationToken);
    }

    /// <summary>
    /// Builds the NATS-specific transport configuration by invoking the user-supplied
    /// configuration delegate on a <see cref="NatsMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> containing all NATS endpoint and pipeline definitions.</returns>
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new NatsMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// Creates a new <see cref="NatsReceiveEndpoint"/> bound to this transport, which will
    /// consume messages from a NATS JetStream consumer.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="NatsReceiveEndpoint"/> for this transport.</returns>
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new NatsReceiveEndpoint(this);
    }

    /// <summary>
    /// Creates a new <see cref="NatsDispatchEndpoint"/> bound to this transport, which will
    /// publish messages to NATS JetStream subjects.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="NatsDispatchEndpoint"/> for this transport.</returns>
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new NatsDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        NatsDispatchEndpointConfiguration? configuration = null;
        if (route.Kind == OutboundRouteKind.Send)
        {
            var subjectName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            configuration = new NatsDispatchEndpointConfiguration
            {
                SubjectName = subjectName,
                Name = "s/" + subjectName
            };
        }
        else if (route.Kind == OutboundRouteKind.Publish)
        {
            var subjectName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            configuration = new NatsDispatchEndpointConfiguration
            {
                SubjectName = subjectName,
                Name = "s/" + subjectName
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        NatsDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new NatsDispatchEndpointConfiguration
                {
                    Kind = DispatchEndpointKind.Reply,
                    SubjectName = instanceEndpointName,
                    Name = "Replies"
                };
            }

            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "s" && name is var subjectSegment)
                {
                    configuration = new NatsDispatchEndpointConfiguration
                    {
                        SubjectName = new string(subjectSegment),
                        Name = "s/" + new string(subjectSegment)
                    };
                }
            }
        }

        if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];
            if (kind is "s" && name is var subjectSegment)
            {
                configuration = new NatsDispatchEndpointConfiguration
                {
                    SubjectName = new string(subjectSegment),
                    Name = "s/" + new string(subjectSegment)
                };
            }
        }

        if (configuration is null && address is { Scheme: "subject" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new NatsDispatchEndpointConfiguration
                {
                    SubjectName = name,
                    Name = "s/" + name
                };
            }
        }

        return configuration;
    }

    /// <inheritdoc />
    public override ReceiveEndpointConfiguration CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route)
    {
        NatsReceiveEndpointConfiguration configuration;
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            configuration = new NatsReceiveEndpointConfiguration
            {
                Name = "Replies",
                SubjectName = instanceEndpointName,
                ConsumerName = instanceEndpointName,
                IsTemporary = true,
                Kind = ReceiveEndpointKind.Reply,
                AutoProvision = true,
                ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
            };
        }
        else
        {
            var endpointName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
            configuration = new NatsReceiveEndpointConfiguration
            {
                Name = endpointName,
                SubjectName = endpointName,
                ConsumerName = endpointName
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (address.Scheme == Schema)
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (Topology.Address.IsBaseOf(address))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Destination.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (address is { Scheme: "subject" })
        {
            string? subjectName = null;

            if (address.Host is { Length: > 0 })
            {
                subjectName = address.Host;
            }
            else
            {
                var subjectPath = address.AbsolutePath.AsSpan();
                Span<Range> subjectRanges = stackalloc Range[2];
                var subjectSegmentCount = subjectPath.Split(
                    subjectRanges, '/', RemoveEmptyEntries | TrimEntries);

                if (subjectSegmentCount == 1)
                {
                    subjectName = new string(subjectPath[subjectRanges[0]]);
                }
            }

            if (subjectName is not null)
            {
                foreach (var candidate in DispatchEndpoints)
                {
                    if (candidate.Destination is NatsSubject subject && subject.Name == subjectName)
                    {
                        endpoint = candidate;
                        return true;
                    }
                }
            }
        }

        endpoint = null;
        return false;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_ownsConnection && _connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
