using Mocha.Features;

namespace Mocha.Sagas;

/// <summary>
/// Carries the saga store for the current consume attempt. It is created on the attempt's own
/// feature collection and is never shared with the receive context or pooled.
/// </summary>
public class SagaFeature
{
    /// <summary>
    /// Gets or sets the saga store used for persisting saga state.
    /// </summary>
    public ISagaStore Store { get; set; } = null!;
}

internal static class ConsumeContextSagaExtensions
{
    extension(IConsumeContext context)
    {
        public SagaFeature GetSagaFeature() => context.Features.GetOrSet<SagaFeature>();
    }
}
