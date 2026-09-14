namespace HotChocolate.CostAnalysis;

/// <summary>
/// A factored join of mutually exclusive decision alternatives.
/// </summary>
internal sealed class JoinDecision<T>(
    BooleanDecision<T> left,
    BooleanDecision<T> right,
    Func<T, T, T> join) : BooleanDecision<T>
{
    public BooleanDecision<T> Left { get; } = left;

    public BooleanDecision<T> Right { get; } = right;

    public Func<T, T, T> JoinOperation { get; } = join;
}
