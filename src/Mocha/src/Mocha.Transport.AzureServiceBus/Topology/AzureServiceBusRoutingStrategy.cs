using Mocha.Features;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Defines the endpoint and topology layout for the Azure Service Bus transport.
/// </summary>
public sealed class AzureServiceBusRoutingStrategy : RoutingStrategy<AzureServiceBusMessagingTransport>
{
    private AzureServiceBusMessagingTopology _topology =>
        field ??= (AzureServiceBusMessagingTopology)Transport.Topology;

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var resolution = AzureServiceBusDestinations.Resolve(Transport.Schema, context.Naming, route);

        if (resolution.Kind == AzureServiceBusDestinationKind.Queue)
        {
            return new AzureServiceBusDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }

        return new AzureServiceBusDispatchEndpointConfiguration
        {
            TopicName = resolution.Name,
            Name = resolution.EndpointName
        };
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        AzureServiceBusDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Transport.Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new AzureServiceBusDispatchEndpointConfiguration
                {
                    Kind = DispatchEndpointKind.Reply,
                    QueueName = instanceEndpointName,
                    Name = "Replies"
                };
            }

            if (segmentCount == 2)
            {
                configuration = CreateResourceEndpointConfiguration(path, ranges);
            }
        }

        if (configuration is null
            && Transport.Topology.Address.IsBaseOf(address)
            && segmentCount == 2)
        {
            configuration = CreateResourceEndpointConfiguration(path, ranges);
        }

        if (configuration is null && TryGetNeutralResourceName(address, "queue", out var queueName))
        {
            configuration = new AzureServiceBusDispatchEndpointConfiguration
            {
                QueueName = queueName,
                Name = "q/" + queueName
            };
        }

        if (configuration is null && TryGetNeutralResourceName(address, "topic", out var topicName))
        {
            configuration = new AzureServiceBusDispatchEndpointConfiguration
            {
                TopicName = topicName,
                Name = "t/" + topicName
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override ReceiveEndpointConfiguration CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route)
    {
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            return new AzureServiceBusReceiveEndpointConfiguration
            {
                Name = "Replies",
                QueueName = instanceEndpointName,
                IsTemporary = true,
                Kind = ReceiveEndpointKind.Reply,
                AutoProvision = true,
                ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
            };
        }

        var queueName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
        return new AzureServiceBusReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
    }

    /// <inheritdoc />
    public override void ConfigureEndpoint(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not AzureServiceBusReceiveEndpointConfiguration azureConfiguration)
        {
            return;
        }

        azureConfiguration.QueueName ??= azureConfiguration.Name;

        if (azureConfiguration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
        {
            var faultFeature = azureConfiguration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
            ConfigureFaultOrSkippedEndpoint(
                context,
                queueName,
                ReceiveEndpointKind.Error,
                faultFeature,
                endpoint => faultFeature.Address ??= endpoint);

            var skippedFeature = azureConfiguration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
            ConfigureFaultOrSkippedEndpoint(
                context,
                queueName,
                ReceiveEndpointKind.Skipped,
                skippedFeature,
                endpoint => skippedFeature.Address ??= endpoint);
        }

        if (Transport.Configuration is AzureServiceBusTransportConfiguration transportConfiguration)
        {
            transportConfiguration.Defaults.Endpoint.ApplyTo(azureConfiguration);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is not AzureServiceBusReceiveEndpoint azureEndpoint
            || configuration is not AzureServiceBusReceiveEndpointConfiguration azureConfiguration)
        {
            return;
        }

        if (azureConfiguration.QueueName is null)
        {
            throw ThrowHelper.ReceiveEndpointQueueNameRequired();
        }

        var forwardDeadLetteredMessagesTo = ResolveNativeDeadLetterDestination(azureConfiguration);

        var queue = _topology.GetOrAddQueue(
            azureConfiguration.QueueName,
            _ => new AzureServiceBusQueueConfiguration
            {
                AutoDelete = azureEndpoint.Kind == ReceiveEndpointKind.Reply,
                AutoDeleteOnIdle = azureEndpoint.Kind == ReceiveEndpointKind.Reply
                    ? TimeSpan.FromHours(24)
                    : null,
                AutoProvision = azureConfiguration.AutoProvision,
                ForwardDeadLetteredMessagesTo = forwardDeadLetteredMessagesTo,
                Origin = TopologyOrigin.Endpoint
            });

        if (forwardDeadLetteredMessagesTo is not null)
        {
            if (queue.ForwardDeadLetteredMessagesTo is not null
                && queue.ForwardDeadLetteredMessagesTo != forwardDeadLetteredMessagesTo)
            {
                throw ThrowHelper.DeadLetterForwardingConflict(
                    azureConfiguration.Name ?? azureConfiguration.QueueName,
                    azureConfiguration.QueueName,
                    queue.ForwardDeadLetteredMessagesTo);
            }

            queue.SetForwardDeadLetteredMessagesTo(forwardDeadLetteredMessagesTo);
        }

        if (azureEndpoint.Kind == ReceiveEndpointKind.Default)
        {
            var inheritedAutoProvision = GetInheritedQueueAutoProvision(
                azureConfiguration.QueueName,
                azureConfiguration);

            EnsureFaultOrSkippedQueue(
                azureConfiguration.Features.Get<ReceiveFaultEndpointFeature>()?.Address,
                inheritedAutoProvision);
            EnsureFaultOrSkippedQueue(
                azureConfiguration.Features.Get<ReceiveSkippedEndpointFeature>()?.Address,
                inheritedAutoProvision);
        }

        if (azureEndpoint.Kind
            is ReceiveEndpointKind.Reply
                or ReceiveEndpointKind.Error
                or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var schema = Transport.Schema;
        var autoBind = (azureConfiguration.BindMode ?? Transport.BindMode) is MessagingBindMode.Implicit;

        foreach (var route in context.Router.GetInboundByEndpoint(azureEndpoint))
        {
            if (route.Kind is InboundRouteKind.Reply || route.MessageType is not { } messageType)
            {
                continue;
            }

            var explicitPublishRoute = context.Router.GetOutboundByMessageType(messageType)
                .FirstOrDefault(r => r is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish });

            if (explicitPublishRoute is not null)
            {
                var destination = AzureServiceBusDestinations.Resolve(schema, context.Naming, explicitPublishRoute);
                if (destination.Kind == AzureServiceBusDestinationKind.Queue)
                {
                    continue;
                }

                EnsureTopic(destination.Name);
                if (autoBind)
                {
                    EnsureSubscription(destination.Name, azureConfiguration.QueueName);
                }

                continue;
            }

            if (!autoBind)
            {
                continue;
            }

            var topicName = context.Naming.GetPublishEndpointName(messageType.RuntimeType);
            EnsureTopic(topicName);
            EnsureSubscription(topicName, azureConfiguration.QueueName);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is not AzureServiceBusDispatchEndpoint
            || configuration is not AzureServiceBusDispatchEndpointConfiguration azureConfiguration)
        {
            return;
        }

        if (Transport.BindMode != MessagingBindMode.Implicit)
        {
            return;
        }

        if (azureConfiguration.TopicName is not null)
        {
            _topology.GetOrAddTopic(
                azureConfiguration.TopicName,
                static _ => new AzureServiceBusTopicConfiguration());
        }

        if (azureConfiguration.QueueName is not null)
        {
            _topology.GetOrAddQueue(
                azureConfiguration.QueueName,
                static _ => new AzureServiceBusQueueConfiguration());
        }
    }

    private static AzureServiceBusDispatchEndpointConfiguration? CreateResourceEndpointConfiguration(
        ReadOnlySpan<char> path,
        Span<Range> ranges)
    {
        var kind = path[ranges[0]];
        var name = new string(path[ranges[1]]);

        return kind switch
        {
            "t" => new AzureServiceBusDispatchEndpointConfiguration
            {
                TopicName = name,
                Name = "t/" + name
            },
            "q" => new AzureServiceBusDispatchEndpointConfiguration
            {
                QueueName = name,
                Name = "q/" + name
            },
            _ => null
        };
    }

    private static bool TryGetNeutralResourceName(Uri address, string scheme, out string name)
    {
        if (address.Scheme != scheme)
        {
            name = string.Empty;
            return false;
        }

        if (!string.IsNullOrEmpty(address.Host) && address.AbsolutePath is "" or "/")
        {
            name = address.Host;
            return true;
        }

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[1];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);
        if (segmentCount == 1)
        {
            name = new string(path[ranges[0]]);
            return true;
        }

        name = string.Empty;
        return false;
    }

    private void EnsureTopic(string topicName)
    {
        _topology.GetOrAddTopic(
            topicName,
            static _ => new AzureServiceBusTopicConfiguration
            {
                Origin = TopologyOrigin.Convention
            });
    }

    private void EnsureSubscription(string sourceTopicName, string queueName)
    {
        _topology.EnsureSubscription(
            sourceTopicName,
            queueName,
            static (_, _) => new AzureServiceBusSubscriptionConfiguration
            {
                Origin = TopologyOrigin.Convention
            });
    }

    private void ConfigureFaultOrSkippedEndpoint(
        IMessagingConfigurationContext context,
        string queueName,
        ReceiveEndpointKind kind,
        ReceiveFaultEndpointFeature feature,
        Action<Uri> assign)
    {
        if (feature.IsDisabled)
        {
            return;
        }

        if (feature.Address is null)
        {
            var name = context.Naming.GetReceiveEndpointName(queueName, kind);
            assign(new Uri($"{Transport.Schema}:q/{name}"));
        }
    }

    private void ConfigureFaultOrSkippedEndpoint(
        IMessagingConfigurationContext context,
        string queueName,
        ReceiveEndpointKind kind,
        ReceiveSkippedEndpointFeature feature,
        Action<Uri> assign)
    {
        if (feature.IsDisabled)
        {
            return;
        }

        if (feature.Address is null)
        {
            var name = context.Naming.GetReceiveEndpointName(queueName, kind);
            assign(new Uri($"{Transport.Schema}:q/{name}"));
        }
    }

    private void EnsureFaultOrSkippedQueue(Uri? address, bool? inheritedAutoProvision)
    {
        if (address is null || !TryGetQueueName(address, out var queueName))
        {
            return;
        }

        _topology.GetOrAddQueue(
            queueName,
            _ => new AzureServiceBusQueueConfiguration
            {
                AutoProvision = inheritedAutoProvision,
                Origin = TopologyOrigin.Endpoint
            });
    }

    private string? ResolveNativeDeadLetterDestination(
        AzureServiceBusReceiveEndpointConfiguration configuration)
    {
        if (!configuration.UseNativeDeadLetterForwarding)
        {
            return null;
        }

        var faultAddress = configuration.Features.Get<ReceiveFaultEndpointFeature>()?.Address;
        if (faultAddress is null || !TryGetQueueName(faultAddress, out var queueName))
        {
            throw new InvalidOperationException(
                $"Receive endpoint '{configuration.Name}' uses native dead-letter forwarding, "
                + "but its fault endpoint is disabled or is not an Azure Service Bus queue.");
        }

        return queueName;
    }

    private bool? GetInheritedQueueAutoProvision(
        string queueName,
        AzureServiceBusReceiveEndpointConfiguration configuration)
        => (Transport.Configuration as AzureServiceBusTransportConfiguration)
            ?.Queues.FirstOrDefault(q => q.Name == queueName)?.AutoProvision
            ?? configuration.AutoProvision;

    private bool TryGetQueueName(Uri address, out string queueName)
    {
        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Transport.Schema && address.Host is "" && segmentCount == 2)
        {
            if (path[ranges[0]] is "q")
            {
                queueName = new string(path[ranges[1]]);
                return true;
            }
        }

        if (Transport.Topology.Address.IsBaseOf(address) && TryGetBaseQueueName(address, out queueName))
        {
            return true;
        }

        return TryGetNeutralResourceName(address, "queue", out queueName);
    }

    private bool TryGetBaseQueueName(Uri address, out string queueName)
    {
        var relative = Transport.Topology.Address.MakeRelativeUri(address);
        if (relative.IsAbsoluteUri)
        {
            queueName = string.Empty;
            return false;
        }

        var relativePath = Uri.UnescapeDataString(relative.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        var path = relativePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (segmentCount == 2 && path[ranges[0]] is "q")
        {
            queueName = new string(path[ranges[1]]);
            return true;
        }

        queueName = string.Empty;
        return false;
    }
}
