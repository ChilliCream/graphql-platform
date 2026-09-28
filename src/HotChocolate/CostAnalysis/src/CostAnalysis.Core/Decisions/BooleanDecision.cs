namespace HotChocolate.CostAnalysis;

/// <summary>
/// A result that depends on Boolean variables, with alternatives for their possible values.
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
    /// Creates a decision that combines mutually exclusive alternatives using <paramref name="join"/>.
    /// </summary>
    public static BooleanDecision<T> Join(
        BooleanDecision<T> left,
        BooleanDecision<T> right,
        Func<T, T, T> join)
        => new JoinDecision<T>(left, right, join);

    /// <summary>
    /// Resolves this decision using the supplied Boolean variable values.
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
    /// Returns an assumed bound covering every possible Boolean assignment using <paramref name="join"/>.
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
    /// Combines the results of two decisions for each Boolean assignment using <paramref name="op"/>.
    /// Each resulting split consumes one case from <paramref name="budget"/>.
    /// When the budget is exhausted, <paramref name="join"/> supplies a bound for unresolved alternatives.
    /// </summary>
    public static BooleanDecision<T> ZipWith(
        BooleanDecision<T> left,
        BooleanDecision<T> right,
        Func<T, T, T> op,
        Func<T, T, T> join,
        CaseBudget budget)
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

        return Split(
            pivot,
            ZipWith(leftFalse, rightFalse, op, join, budget),
            ZipWith(leftTrue, rightTrue, op, join, budget));
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
    /// Returns the decisions for <paramref name="variable"/> set to false and true,
    /// with all references to that variable resolved.
    /// </summary>
    private static (BooleanDecision<T> WhenFalse, BooleanDecision<T> WhenTrue) Branches(
        BooleanDecision<T> node,
        string variable)
        => (Restrict(node, variable, false), Restrict(node, variable, true));

    private static BooleanDecision<T> Restrict(BooleanDecision<T> node, string variable, bool value)
        => node switch
        {
            LeafDecision<T> => node,
            SplitDecision<T> split when split.Variable == variable => value ? split.WhenTrue : split.WhenFalse,
            SplitDecision<T> split => Split(
                split.Variable,
                Restrict(split.WhenFalse, variable, value),
                Restrict(split.WhenTrue, variable, value)),
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
