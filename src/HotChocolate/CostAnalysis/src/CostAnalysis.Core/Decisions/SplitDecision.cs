namespace HotChocolate.CostAnalysis;

/// <summary>
/// A <see cref="BooleanDecision{T}"/> split on one Boolean variable.
/// </summary>
internal sealed class SplitDecision<T>(string variable, BooleanDecision<T> whenFalse, BooleanDecision<T> whenTrue) : BooleanDecision<T>
{
    /// <summary>
    /// Gets the variable this node splits on.
    /// </summary>
    public string Variable { get; } = variable;

    /// <summary>
    /// Gets the branch taken when <see cref="Variable"/> is
    /// <see langword="false"/>.
    /// </summary>
    public BooleanDecision<T> WhenFalse { get; } = whenFalse;

    /// <summary>
    /// Gets the branch taken when <see cref="Variable"/> is
    /// <see langword="true"/>.
    /// </summary>
    public BooleanDecision<T> WhenTrue { get; } = whenTrue;
}
