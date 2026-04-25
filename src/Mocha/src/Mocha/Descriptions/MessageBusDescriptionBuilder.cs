using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// A visitor that traverses a <see cref="MessagingRuntime"/> and builds a <see cref="MessageBusDescription"/> for diagnostic output.
/// </summary>
/// <remarks>
/// <para>
/// This visitor and the <c>*Description</c> record tree it produces are kept public for the
/// duration of release N so the deprecated <c>MapMessageBusDeveloperTopology</c> bridge can keep
/// emitting the legacy JSON shape from the same intermediate. They are scheduled for
/// internalization in the next major — new code should consume <c>MochaResource</c> via
/// <c>MochaResourceSource</c> instead.
/// </para>
/// </remarks>
public sealed class MessageBusDescriptionVisitor : MessagingVisitor<MessageBusDescriptionVisitor.Context>
{
    /// <summary>
    /// Visits the specified runtime and returns a complete diagnostic description.
    /// </summary>
    /// <param name="runtime">The messaging runtime to describe.</param>
    /// <returns>A <see cref="MessageBusDescription"/> containing the full bus topology and configuration.</returns>
    public static MessageBusDescription Visit(MessagingRuntime runtime)
    {
        var context = new Context();
        Instance.Visit(runtime, context);
        return context.ToDescription();
    }

    /// <summary>
    /// Accumulates visitor results during traversal of the messaging runtime.
    /// </summary>
    public sealed class Context
    {
        internal HostDescription? Host { get; set; }
        internal List<MessageTypeDescription> MessageTypes { get; } = [];
        internal List<ConsumerDescription> Consumers { get; } = [];
        internal List<InboundRouteDescription> InboundRoutes { get; } = [];
        internal List<OutboundRouteDescription> OutboundRoutes { get; } = [];
        internal List<TransportDescription> Transports { get; } = [];
        internal List<SagaDescription>? Sagas { get; set; }

        internal MessageBusDescription ToDescription()
            => new(
                Host ?? throw ThrowHelper.HostDescriptionMissing(),
                MessageTypes,
                Consumers,
                new RoutesDescription(InboundRoutes, OutboundRoutes),
                Transports,
                Sagas is { Count: > 0 } ? Sagas : null);
    }

    protected override VisitorAction Enter(MessagingRuntime runtime, Context context)
    {
        context.Host = new HostDescription(
            runtime.Host.ServiceName,
            runtime.Host.AssemblyName,
            runtime.Host.InstanceId.ToString("D"));

        return VisitorAction.Continue;
    }

    protected override VisitorAction Enter(MessageType messageType, Context context)
    {
        context.MessageTypes.Add(messageType.Describe());
        return VisitorAction.Continue;
    }

    protected override VisitorAction Enter(Consumer consumer, Context context)
    {
        context.Consumers.Add(consumer.Describe());
        return VisitorAction.Continue;
    }

    protected override VisitorAction Enter(InboundRoute route, Context context)
    {
        context.InboundRoutes.Add(route.Describe());
        return VisitorAction.Continue;
    }

    protected override VisitorAction Enter(OutboundRoute route, Context context)
    {
        context.OutboundRoutes.Add(route.Describe());
        return VisitorAction.Continue;
    }

    protected override VisitorAction Enter(MessagingTransport transport, Context context)
    {
        context.Transports.Add(transport.Describe());
        return VisitorAction.Skip;
    }

    protected override VisitorAction Enter(Saga saga, Context context)
    {
        context.Sagas ??= [];
        context.Sagas.Add(saga.Describe());
        return VisitorAction.Skip;
    }

    /// <summary>
    /// Gets the singleton instance of the description visitor.
    /// </summary>
    public static MessageBusDescriptionVisitor Instance { get; } = new MessageBusDescriptionVisitor();
}
