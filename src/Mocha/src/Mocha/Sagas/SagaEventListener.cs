using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Sagas;

/// <summary>
/// A consumer that routes incoming messages to the appropriate saga state machine for processing.
/// </summary>
/// <param name="saga">The saga definition that this consumer handles.</param>
public sealed class SagaConsumer(Saga saga) : Consumer
{
    /// <summary>
    /// Gets the saga definition that this consumer handles.
    /// </summary>
    public Saga Saga => saga;

    /// <inheritdoc />
    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor.Name(saga.Name);

        foreach (var state in saga.States)
        {
            foreach (var transition in state.Value.Transitions)
            {
                var eventType = transition.Key;
                var transitionKind = transition.Value.TransitionKind;

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
                        // A reply lands on the shared reply endpoint alongside non saga (RPC) replies.
                        // The saga-id header marks replies to the saga's own requests, so the route
                        // selects only those. A typed reply additionally narrows by its message type.
                        r.Condition(CreateReplyCondition(eventType));
                    }
                });
            }
        }
    }

    private static RouteCondition CreateReplyCondition(Type eventType)
    {
        // The saga-id header is the discriminator: only replies to the saga's own requests carry it,
        // so the route never selects a non saga (RPC) reply on the shared reply endpoint.
        var sagaId = new HeaderPresentCondition<string>(SagaContextData.SagaId);

        // OnAnyReply (OnReply<object>) routes every saga-id reply from the endpoint to the saga
        // consumer, which then correlates by id.
        if (eventType == typeof(object))
        {
            return sagaId;
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
            Name,
            DescriptionHelpers.GetTypeName(Identity),
            Identity.FullName,
            saga.Name,
            false);
    }

    /// <inheritdoc />
    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);
        SetIdentity(saga.GetType());
    }

    /// <inheritdoc />
    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var sagaFeature = context.Features.GetOrSet<SagaFeature>();
        sagaFeature.Store ??= context.Services.GetRequiredService<ISagaStore>();

        var ct = context.CancellationToken;

        await using var transaction = await sagaFeature.Store.StartTransactionAsync(ct);

        await saga.HandleEvent(context);

        await transaction.CommitAsync(ct);
    }
}
