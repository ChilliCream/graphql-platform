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
    /// Creates a factored join of mutually exclusive alternatives.
    /// </summary>
    public static BooleanDecision<T> Join(
        BooleanDecision<T> left,
        BooleanDecision<T> right,
        Func<T, T, T> join)
        => new JoinDecision<T>(left, right, join);

    /// <summary>
    /// Resolves this decision against a complete Boolean assignment by
    /// walking one branch per split.
    /// </summary>
    public T Resolve(Func<string, bool> values)
        => this switch
        {
            LeafDecision<T> leaf => leaf.Value,
            SplitDecision<T> split => (values(split.Variable) ? split.WhenTrue : split.WhenFalse).Resolve(values),
            JoinDecision<T> joined => joined.JoinOperation(joined.Left.Resolve(values), joined.Right.Resolve(values)),
            _ => throw ThrowHelper.UnexpectedDecision()
        };

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
            JoinDecision<T> joined => join(joined.Left.FoldWithJoin(join), joined.Right.FoldWithJoin(join)),
            _ => throw ThrowHelper.UnexpectedDecision()
        };

    /// <summary>
    /// Combines two decisions pointwise with <paramref name="op"/>, an
    /// ordered BDD apply over the engine's single canonical (ordinal)
    /// variable order that pairs every occurrence of one variable with
    /// itself. Charges one case per split it materializes against
    /// <paramref name="budget"/> and collapses both sides with
    /// <paramref name="join"/> into a leaf once the budget is exhausted.
    /// </summary>
    public static BooleanDecision<T> ZipWith(BooleanDecision<T> left, BooleanDecision<T> right, Func<T, T, T> op, Func<T, T, T> join, CaseBudget budget)
    {
        if (TryCollapseResolved(left, out var leftValue)
            && TryCollapseResolved(right, out var rightValue))
        {
            return Leaf(op(leftValue, rightValue));
        }

        if (!budget.TrySpend())
        {
            return Leaf(op(left.FoldWithJoin(join), right.FoldWithJoin(join)));
        }

        var pivot = PickPivot(left, right);
        var (leftFalse, leftTrue) = Branches(left, pivot);
        var (rightFalse, rightTrue) = Branches(right, pivot);

        return Split(pivot, ZipWith(leftFalse, rightFalse, op, join, budget), ZipWith(leftTrue, rightTrue, op, join, budget));
    }

    private static string PickPivot(BooleanDecision<T> left, BooleanDecision<T> right)
    {
        string? pivot = null;
        FindPivot(left, ref pivot);
        FindPivot(right, ref pivot);
        return pivot!;
    }

    private static void FindPivot(BooleanDecision<T> node, ref string? pivot)
    {
        switch (node)
        {
            case SplitDecision<T> split:
                if (pivot is null || string.CompareOrdinal(split.Variable, pivot) < 0)
                {
                    pivot = split.Variable;
                }

                FindPivot(split.WhenFalse, ref pivot);
                FindPivot(split.WhenTrue, ref pivot);
                break;

            case JoinDecision<T> joined:
                FindPivot(joined.Left, ref pivot);
                FindPivot(joined.Right, ref pivot);
                break;
        }
    }

    /// <summary>
    /// Restricts <paramref name="node"/> to <paramref name="variable"/>'s
    /// false and true branches, eliminating every split on that variable
    /// anywhere in the subtree rather than only at its top.
    /// </summary>
    private static (BooleanDecision<T> WhenFalse, BooleanDecision<T> WhenTrue) Branches(BooleanDecision<T> node, string variable)
        => (Restrict(node, variable, false), Restrict(node, variable, true));

    private static BooleanDecision<T> Restrict(BooleanDecision<T> node, string variable, bool value)
        => node switch
        {
            LeafDecision<T> => node,
            SplitDecision<T> split when split.Variable == variable => value ? split.WhenTrue : split.WhenFalse,
            SplitDecision<T> split => Split(split.Variable, Restrict(split.WhenFalse, variable, value), Restrict(split.WhenTrue, variable, value)),
            JoinDecision<T> joined => Join(
                Restrict(joined.Left, variable, value),
                Restrict(joined.Right, variable, value),
                joined.JoinOperation),
            _ => throw ThrowHelper.UnexpectedDecision()
        };

    private static bool TryCollapseResolved(BooleanDecision<T> node, out T value)
    {
        switch (node)
        {
            case LeafDecision<T> leaf:
                value = leaf.Value;
                return true;

            case JoinDecision<T> joined
                when TryCollapseResolved(joined.Left, out var left)
                    && TryCollapseResolved(joined.Right, out var right):
                value = joined.JoinOperation(left, right);
                return true;

            default:
                value = default!;
                return false;
        }
    }
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
