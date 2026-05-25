using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Base class for implementing the visitor pattern over an <see cref="IMessagingRuntime"/> graph, traversing message types, consumers, routes, transports, endpoints, and sagas.
/// </summary>
/// <typeparam name="TContext">The type of context carried through the visitor traversal.</typeparam>
internal abstract class MessagingVisitor<TContext>
{
    /// <summary>
    /// Begins visiting the specified messaging runtime with the given context.
    /// </summary>
    /// <param name="runtime">The messaging runtime to traverse.</param>
    /// <param name="context">The visitor context to accumulate results.</param>
    public void Visit(MessagingRuntime runtime, TContext context)
        => Visit((IMessagingRuntime)runtime, context);

    /// <summary>
    /// Begins visiting the specified messaging runtime with the given context.
    /// </summary>
    /// <param name="runtime">The messaging runtime to traverse.</param>
    /// <param name="context">The visitor context to accumulate results.</param>
    public void Visit(IMessagingRuntime runtime, TContext context)
    {
        if (Enter(runtime, context) == VisitorAction.Break)
        {
            return;
        }

        VisitChildren(runtime, context);
        Leave(runtime, context);
    }

    protected virtual void VisitChildren(IMessagingRuntime runtime, TContext context)
    {
        foreach (var messageType in runtime.Messages.MessageTypes)
        {
            var action = Enter(messageType, context);
            if (action == VisitorAction.Break)
            {
                return;
            }

            if (action != VisitorAction.Skip)
            {
                Leave(messageType, context);
            }
        }

        foreach (var consumer in runtime.Consumers)
        {
            var action = Enter(consumer, context);
            if (action == VisitorAction.Break)
            {
                return;
            }

            if (action != VisitorAction.Skip)
            {
                Leave(consumer, context);
            }
        }

        foreach (var route in runtime.Router.InboundRoutes)
        {
            var action = Enter(route, context);
            if (action == VisitorAction.Break)
            {
                return;
            }

            if (action != VisitorAction.Skip)
            {
                Leave(route, context);
            }
        }

        foreach (var route in runtime.Router.OutboundRoutes)
        {
            var action = Enter(route, context);
            if (action == VisitorAction.Break)
            {
                return;
            }

            if (action != VisitorAction.Skip)
            {
                Leave(route, context);
            }
        }

        foreach (var transport in runtime.Transports)
        {
            if (VisitTransport(transport, context) == VisitorAction.Break)
            {
                return;
            }
        }

        foreach (var consumer in runtime.Consumers)
        {
            if (consumer is SagaConsumer sagaConsumer)
            {
                if (VisitSaga(sagaConsumer, context) == VisitorAction.Break)
                {
                    return;
                }
            }
        }
    }

    protected virtual VisitorAction VisitTransport(MessagingTransport transport, TContext context)
    {
        var action = Enter(transport, context);
        if (action == VisitorAction.Break)
        {
            return VisitorAction.Break;
        }

        if (action == VisitorAction.Skip)
        {
            return VisitorAction.Continue;
        }

        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            action = Enter(endpoint, context);
            if (action == VisitorAction.Break)
            {
                return VisitorAction.Break;
            }

            if (action != VisitorAction.Skip)
            {
                Leave(endpoint, context);
            }
        }

        foreach (var endpoint in transport.DispatchEndpoints)
        {
            action = Enter(endpoint, context);
            if (action == VisitorAction.Break)
            {
                return VisitorAction.Break;
            }

            if (action != VisitorAction.Skip)
            {
                Leave(endpoint, context);
            }
        }

        Leave(transport, context);
        return VisitorAction.Continue;
    }

    protected virtual VisitorAction VisitSaga(SagaConsumer consumer, TContext context)
    {
        var saga = GetSagaFromConsumer(consumer);
        if (saga is null)
        {
            return VisitorAction.Continue;
        }

        var action = Enter(saga, context);
        if (action == VisitorAction.Break)
        {
            return VisitorAction.Break;
        }

        if (action == VisitorAction.Skip)
        {
            return VisitorAction.Continue;
        }

        Leave(saga, context);
        return VisitorAction.Continue;
    }

    private static Saga? GetSagaFromConsumer(SagaConsumer consumer)
    {
        var fields = typeof(SagaConsumer).GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (typeof(Saga).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(consumer) as Saga;
            }
        }

        return null;
    }

    protected virtual VisitorAction Enter(IMessagingRuntime runtime, TContext context)
        => runtime is MessagingRuntime messagingRuntime ? Enter(messagingRuntime, context) : VisitorAction.Continue;

    protected virtual VisitorAction Enter(MessagingRuntime runtime, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(IMessagingRuntime runtime, TContext context)
        => runtime is MessagingRuntime messagingRuntime ? Leave(messagingRuntime, context) : VisitorAction.Continue;

    protected virtual VisitorAction Leave(MessagingRuntime runtime, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(MessageType messageType, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(MessageType messageType, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(Consumer consumer, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(Consumer consumer, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(InboundRoute route, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(InboundRoute route, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(OutboundRoute route, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(OutboundRoute route, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(MessagingTransport transport, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(MessagingTransport transport, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(ReceiveEndpoint endpoint, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(ReceiveEndpoint endpoint, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(DispatchEndpoint endpoint, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(DispatchEndpoint endpoint, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Enter(Saga saga, TContext context) => VisitorAction.Continue;

    protected virtual VisitorAction Leave(Saga saga, TContext context) => VisitorAction.Continue;
}
