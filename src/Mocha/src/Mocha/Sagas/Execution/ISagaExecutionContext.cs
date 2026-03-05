using Microsoft.Extensions.Logging;
using Mocha;
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

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Store = null!;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Store = null!;
    }
}

internal static class ConsumeContextSagaExtensions
{
    extension(IConsumeContext context)
    {
        public SagaFeature GetSagaFeature() => context.Features.GetOrSet<SagaFeature>();
    }
}
