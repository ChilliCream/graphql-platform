namespace HotChocolate.CostAnalysis;

/// <summary>
/// A persistent, lazily factored decision structure over Boolean variables,
/// split in the engine's canonical (ordinal) variable order: either a
/// resolved leaf value or a split on one variable into its false and true
/// branches.
/// </summary>
/// <typeparam name="T">
/// The value type held at every leaf.
/// </typeparam>
internal abstract class BooleanDecision<T>
{
    /// <summary>
    /// Creates a resolved leaf holding <paramref name="value"/>.
    /// </summary>
    public static BooleanDecision<T> Leaf(T value) => new LeafDecision<T>(value);

    /// <summary>
    /// Creates a split on <paramref name="variable"/> with its false and
    /// true branches.
    /// </summary>
    public static BooleanDecision<T> Split(string variable, BooleanDecision<T> whenFalse, BooleanDecision<T> whenTrue)
        => new SplitDecision<T>(variable, whenFalse, whenTrue);

    /// <summary>
    /// Resolves this decision against a complete Boolean assignment by
    /// walking one branch per split.
    /// </summary>
    public T Resolve(Func<string, bool> values)
    {
        var node = this;

        while (node is SplitDecision<T> split)
        {
            node = values(split.Variable) ? split.WhenTrue : split.WhenFalse;
        }

        return ((LeafDecision<T>)node).Value;
    }

    /// <summary>
    /// Folds every leaf into one value by applying <paramref name="join"/>
    /// bottom-up, the static bound of this decision over every possible
    /// assignment.
    /// </summary>
    public T FoldWithJoin(Func<T, T, T> join)
        => this switch
        {
            LeafDecision<T> leaf => leaf.Value,
            SplitDecision<T> split => join(split.WhenFalse.FoldWithJoin(join), split.WhenTrue.FoldWithJoin(join)),
            _ => throw new NotSupportedException()
        };

    /// <summary>
    /// Combines two decisions pointwise with <paramref name="op"/>, an
    /// ordered BDD apply that keeps each side's independent variable
    /// support factored. A side not split on the pivot variable broadcasts
    /// unchanged into both of the pivot's branches.
    /// </summary>
    public static BooleanDecision<T> ZipWith(BooleanDecision<T> left, BooleanDecision<T> right, Func<T, T, T> op)
    {
        if (left is LeafDecision<T> leftLeaf && right is LeafDecision<T> rightLeaf)
        {
            return Leaf(op(leftLeaf.Value, rightLeaf.Value));
        }

        var pivot = PickPivot(left, right);
        var (leftFalse, leftTrue) = Branches(left, pivot);
        var (rightFalse, rightTrue) = Branches(right, pivot);

        return Split(pivot, ZipWith(leftFalse, rightFalse, op), ZipWith(leftTrue, rightTrue, op));
    }

    private static string PickPivot(BooleanDecision<T> left, BooleanDecision<T> right)
    {
        if (left is SplitDecision<T> leftSplit
            && (right is not SplitDecision<T> rightSplit || string.CompareOrdinal(leftSplit.Variable, rightSplit.Variable) <= 0))
        {
            return leftSplit.Variable;
        }

        return ((SplitDecision<T>)right).Variable;
    }

    private static (BooleanDecision<T> WhenFalse, BooleanDecision<T> WhenTrue) Branches(BooleanDecision<T> node, string variable)
        => node is SplitDecision<T> split && split.Variable == variable
            ? (split.WhenFalse, split.WhenTrue)
            : (node, node);
}

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
