using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;

namespace Mocha.Sagas;

/// <summary>
/// A consumer that routes incoming messages to the appropriate saga state machine for processing.
/// </summary>
/// <param name="saga">The saga definition that this consumer handles.</param>
public sealed class SagaConsumer(Saga saga) : Consumer(saga.GetType())
{
    /// <summary>
    /// Gets the saga definition that this consumer handles.
    /// </summary>
    public Saga Saga => saga;

    /// <inheritdoc />
    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor.Name(saga.Name);

        var hasReplyTransition = false;
        var hasAcknowledgementTransition = false;

        foreach (var state in saga.States)
        {
            foreach (var transition in state.Value.Transitions)
            {
                var eventType = transition.Key;
                var transitionKind = transition.Value.TransitionKind;

                if (transitionKind == SagaTransitionKind.Reply)
                {
                    hasReplyTransition = true;
                    hasAcknowledgementTransition |= eventType.IsAssignableFrom(typeof(AcknowledgedEvent));
                }

                descriptor.AddRoute(r =>
                {
                    r.MessageType(eventType)
                        .Kind(
                            transitionKind switch
                            {
                                SagaTransitionKind.Event => InboundRouteKind.Subscribe,
                                SagaTransitionKind.Send => InboundRouteKind.Send,
                                SagaTransitionKind.Request => InboundRouteKind.Request,
                                SagaTransitionKind.Reply => InboundRouteKind.Reply,
                                _ => throw new InvalidOperationException($"Invalid transition kind: {transitionKind}")
                            });

                    if (transitionKind == SagaTransitionKind.Reply)
                    {
                        // The saga-id header marks replies to the saga's own requests, so the route
                        // selects only those. A typed reply additionally narrows by its message type.
                        r.Condition(CreateReplyCondition(eventType));
                    }
                });
            }
        }

        // A saga with a reply transition receives the acknowledgements of its commands on its own
        // endpoint, including those that no transition handles.
        if (hasReplyTransition && !hasAcknowledgementTransition)
        {
            descriptor.AddRoute(r =>
                r.MessageType(typeof(AcknowledgedEvent))
                    .Kind(InboundRouteKind.Reply)
                    .Condition(CreateReplyCondition(typeof(AcknowledgedEvent))));
        }
    }

    private static RouteCondition CreateReplyCondition(Type eventType)
    {
        // The saga-id header is the discriminator: only replies to the saga's own requests carry it,
        // so the route never selects a non saga (RPC) reply on the shared reply endpoint.
        var sagaId = new HeaderPresentCondition<string>(SagaContextData.SagaId);

        // OnAnyReply routes successful replies to the saga consumer. Fault notifications are
        // selected only by an explicit OnFault route.
        if (eventType == typeof(object))
        {
            return AndCondition.Create(sagaId, NotFaultCondition.Instance);
        }

        // A typed OnReply<T> requires the saga-id and, when the received message resolves a message
        // type, requires it to match the reply type. A reply with no resolved message type still
        // selects the route on the saga-id alone.
        return AndCondition.Create(sagaId, new MessageTypeCondition(eventType, optional: true));
    }

    /// <inheritdoc />
    public override ConsumerDescription Describe()
    {
        return new ConsumerDescription(
            Urn,
            Name,
            DescriptionHelpers.GetTypeName(Identity),
            Identity.FullName,
            saga.Name,
            false,
            Configuration?.Source ?? saga.Configuration?.Source);
    }

    /// <inheritdoc />
    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var sagaFeature = context.Features.GetOrSet<SagaFeature>();
        sagaFeature.Store ??= context.Services.GetRequiredService<ISagaStore>();

        var ct = context.CancellationToken;

        // a retried attempt starts without the messages of the failed attempt
        sagaFeature.PendingDispatches.Clear();

        await using var transaction = await sagaFeature.Store.StartTransactionAsync(ct);

        await saga.HandleEvent(context);

        await transaction.CommitAsync(ct);

        // The messages leave after the state is committed, so a reply cannot arrive before the state
        // that handles it exists.
        var bus = context.GetBus();
        foreach (var dispatch in sagaFeature.PendingDispatches)
        {
            await dispatch(bus, ct);
        }
    }
}
