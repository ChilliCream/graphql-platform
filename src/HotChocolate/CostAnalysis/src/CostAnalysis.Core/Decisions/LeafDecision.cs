namespace HotChocolate.CostAnalysis;

/// <summary>
/// A resolved <see cref="BooleanDecision{T}"/> leaf.
/// </summary>
internal sealed class LeafDecision<T>(T value) : BooleanDecision<T>
{
    /// <summary>
    /// Gets the leaf's value.
    /// </summary>
    public T Value { get; } = value;
}
