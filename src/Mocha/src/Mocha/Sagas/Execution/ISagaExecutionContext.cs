using Mocha.Features;

namespace Mocha.Sagas;

/// <summary>
/// A pooled feature that provides access to the saga store during saga event processing.
/// </summary>
public class SagaFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the saga store used for persisting saga state.
    /// </summary>
    public ISagaStore Store { get; set; } = null!;

    /// <summary>
    /// Gets the messages that the saga produced in the current attempt, in the order it produced them.
    /// They are dispatched after the saga transaction is committed.
    /// </summary>
    internal List<Func<IMessageBus, CancellationToken, ValueTask>> PendingDispatches { get; } = [];

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Store = null!;
        PendingDispatches.Clear();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Store = null!;
        PendingDispatches.Clear();
    }
}

internal static class ConsumeContextSagaExtensions
{
    extension(IConsumeContext context)
    {
        public SagaFeature GetSagaFeature() => context.Features.GetOrSet<SagaFeature>();
    }
}
