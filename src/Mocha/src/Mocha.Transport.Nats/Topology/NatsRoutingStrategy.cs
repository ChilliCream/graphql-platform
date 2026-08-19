using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Transport.Nats;

/// <summary>
/// Resolves Mocha routes into JetStream subjects and durable consumers.
/// </summary>
public sealed class NatsRoutingStrategy : RoutingStrategy<NatsMessagingTransport>
{
    private NatsMessagingTopology Topology => (NatsMessagingTopology)Transport.Topology;

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var subject = NatsDestinations.Resolve(Transport.Schema, context.Naming, route);

        return new NatsDispatchEndpointConfiguration
        {
            Subject = subject,
            Name = NatsAddress.SubjectSegment + "/" + subject
        };
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        var segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (address.Scheme == Transport.Schema
            && address.Host is ""
            && segments is ["replies"])
        {
            return new NatsDispatchEndpointConfiguration
            {
                Kind = DispatchEndpointKind.Reply,
                Subject = context.Naming.GetInstanceEndpoint(context.Host.InstanceId),
                Name = "Replies"
            };
        }

        if (NatsDestinations.TryResolveExplicit(Transport.Schema, address, out var subject))
        {
            return new NatsDispatchEndpointConfiguration
            {
                Subject = subject,
                Name = NatsAddress.SubjectSegment + "/" + subject
            };
        }

        return null;
    }

    /// <inheritdoc />
    public override ReceiveEndpointConfiguration CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route)
    {
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);

            return new NatsReceiveEndpointConfiguration
            {
                Name = "Replies",
                ConsumerName = NatsNaming.ToDurableName(instanceEndpointName),
                FilterSubjects = [instanceEndpointName],
                IsTemporary = true,
                Kind = ReceiveEndpointKind.Reply,
                AutoProvision = true,

                // Without this the response arrives on the inbox subscription but is never
                // correlated back to the pending request, so the caller waits out its timeout.
                ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
            };
        }

        var endpointName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);

        return new NatsReceiveEndpointConfiguration
        {
            Name = endpointName,
            ConsumerName = NatsNaming.ToDurableName(endpointName)
        };
    }

    /// <inheritdoc />
    public override void ConfigureEndpoint(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not NatsReceiveEndpointConfiguration natsConfiguration)
        {
            return;
        }

        natsConfiguration.ConsumerName ??= NatsNaming.ToDurableName(natsConfiguration.Name!);

        if (natsConfiguration is not { Kind: ReceiveEndpointKind.Default, Name: { } endpointName })
        {
            return;
        }

        var fault = natsConfiguration.Features.GetOrSet<ReceiveFaultEndpointFeature>();

        if (!fault.IsDisabled && fault.Address is null)
        {
            fault.Address = FaultAddress(context, endpointName, ReceiveEndpointKind.Error);
        }

        var skipped = natsConfiguration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();

        if (!skipped.IsDisabled && skipped.Address is null)
        {
            skipped.Address = FaultAddress(context, endpointName, ReceiveEndpointKind.Skipped);
        }
    }

    /// <summary>
    /// Builds the address faults or skipped messages are forwarded to, scoped to this service.
    /// </summary>
    // The name an endpoint derives from its handler already carries the service, but one the caller
    // named does not, and an unscoped subject sits at the root of the subject space where every
    // service naming an endpoint the same way claims it. Stream subjects have to be disjoint, so that
    // is a start-up failure for whoever provisions second, or shared fault traffic if they do not.
    private Uri FaultAddress(
        IMessagingConfigurationContext context,
        string endpointName,
        ReceiveEndpointKind kind)
    {
        var name = context.Naming.GetReceiveEndpointName(endpointName, kind);

        if (!name.Contains('.') && context.Host is { } host)
        {
            // Through the naming conventions rather than formatted here, so the prefix matches the one
            // a handler-derived endpoint name already carries.
            var scope = context.Naming.GetReceiveEndpointName(
                host.EffectiveServiceName,
                ReceiveEndpointKind.Default);

            name = scope + "." + name;
        }

        return new Uri($"{Transport.Schema}:{NatsAddress.SubjectSegment}/{name}");
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not NatsReceiveEndpointConfiguration natsConfiguration)
        {
            return;
        }

        if (natsConfiguration.ConsumerName is null)
        {
            throw new InvalidOperationException("Consumer name is required.");
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply)
        {
            if (natsConfiguration.FilterSubjects.FirstOrDefault() is { } replySubject)
            {
                Topology.AddSubject(new NatsSubjectConfiguration
                {
                    Subject = replySubject,
                    IsCore = true,
                    Origin = TopologyOrigin.Endpoint
                });
            }

            return;
        }

        // A filter subject derived from a route is the JetStream equivalent of a convention bind, so
        // explicit binding suppresses it and the consumer reads only what Subject() named.
        if (ResolveBindMode(natsConfiguration) is MessagingBindMode.Implicit)
        {
            CollectFilterSubjects(context, endpoint, natsConfiguration);
        }

        // A consumer can only read subjects its stream captures, so what an endpoint filters has to be
        // captured too, including a subject no dispatch endpoint happens to produce.
        foreach (var subject in natsConfiguration.FilterSubjects)
        {
            Topology.AddSubject(new NatsSubjectConfiguration
            {
                Subject = subject,
                StreamName = natsConfiguration.StreamName ?? Topology.FindStreamForSubject(subject)?.Name,
                Origin = TopologyOrigin.Endpoint
            });
        }

        EnsureFaultSubject(natsConfiguration.Features.Get<ReceiveFaultEndpointFeature>()?.Address);
        EnsureFaultSubject(natsConfiguration.Features.Get<ReceiveSkippedEndpointFeature>()?.Address);

        // MaxConcurrency is deliberately not mapped onto MaxAckPending: one bounds this process, the
        // other is the ceiling shared by every instance reading the durable.
        Topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = natsConfiguration.ConsumerName,
            StreamName = natsConfiguration.StreamName,
            FilterSubjects = natsConfiguration.FilterSubjects,
            AutoProvision = natsConfiguration.AutoProvision,
            AckWait = natsConfiguration.AckWait,
            MaxDeliver = natsConfiguration.MaxDeliver,
            Backoff = natsConfiguration.Backoff,
            MaxAckPending = natsConfiguration.MaxAckPending,
            AckProgressInterval = natsConfiguration.AckProgressInterval,
            DeliverPolicy = natsConfiguration.DeliverPolicy,
            Origin = TopologyOrigin.Endpoint
        });
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (configuration is not NatsDispatchEndpointConfiguration { Subject: { } subject } natsConfiguration)
        {
            return;
        }

        var isReply = endpoint.Kind is DispatchEndpointKind.Reply;

        // Registered whatever the bind mode, because the endpoint resolves its destination from the
        // subject. Under explicit binding it is the convention stream that stands down, so the subject
        // exists without anything claiming it.
        Topology.AddSubject(new NatsSubjectConfiguration
        {
            Subject = subject,
            StreamName = isReply
                ? null
                : Topology.FindStreamForSubject(subject)?.Name,
            IsCore = isReply,
            Origin = TopologyOrigin.Endpoint
        });
    }

    /// <summary>
    /// Resolves the bind mode for an endpoint, which may override the transport's.
    /// </summary>
    private MessagingBindMode ResolveBindMode(NatsReceiveEndpointConfiguration configuration)
        => configuration.BindMode ?? Transport.BindMode;

    private void EnsureFaultSubject(Uri? address)
    {
        if (address is null
            || !NatsDestinations.TryResolveExplicit(Transport.Schema, address, out var subject))
        {
            return;
        }

        Topology.AddSubject(new NatsSubjectConfiguration
        {
            Subject = subject,
            StreamName = Topology.FindStreamForSubject(subject)?.Name,
            Origin = TopologyOrigin.Convention
        });
    }

    private void CollectFilterSubjects(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        NatsReceiveEndpointConfiguration configuration)
    {
        foreach (var route in context.Router.GetInboundByEndpoint(endpoint))
        {
            if (route.Kind is InboundRouteKind.Reply || route.MessageType is not { } messageType)
            {
                continue;
            }

            // Deliberately independent of the route kind. The kind records how the handler was
            // registered, not how a sender dispatches, so deriving the subject from it would filter
            // the wrong one whenever an event handler is sent to, or a request handler published to.
            //
            // The route's own message type only. A handler bound to an interface gets that type's
            // subject, which nothing publishes to, so such an endpoint has to name the concrete
            // subjects with Subject(): message types are not completed until after this runs.
            var subject = NatsDestinations.ResolveConvention(
                context.Naming,
                OutboundRouteKind.Publish,
                messageType);

            if (!configuration.FilterSubjects.Contains(subject))
            {
                configuration.FilterSubjects.Add(subject);
            }
        }
    }
}
