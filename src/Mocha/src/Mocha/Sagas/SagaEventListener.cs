using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Sagas;

/// <summary>
/// A consumer that routes incoming messages to the appropriate saga state machine for processing.
/// </summary>
/// <param name="saga">The saga definition that this consumer handles.</param>
public sealed class SagaConsumer(Saga saga) : Consumer
{
    /// <inheritdoc />
    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor.Name(saga.Name);

        foreach (var state in saga.States)
        {
            foreach (var transition in state.Value.Transitions)
            {
                descriptor.AddRoute(r =>
                    r.MessageType(transition.Key)
                        .Kind(
                            transition.Value.TransitionKind switch
                            {
                                SagaTransitionKind.Event => InboundRouteKind.Subscribe,
                                SagaTransitionKind.Send => InboundRouteKind.Send,
                                SagaTransitionKind.Request => InboundRouteKind.Request,
                                SagaTransitionKind.Reply => InboundRouteKind.Reply,
                                _ => throw new InvalidOperationException(
                                    $"Invalid transition kind: {transition.Value.TransitionKind}")
                            })
                );
            }
        }
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
