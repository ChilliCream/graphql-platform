using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one canonical request-constant policy expression in an operation plan.
/// </summary>
public sealed record PolicyConditionExpression
{
    /// <summary>
    /// Gets the zero-based, plan-local expression ordinal.
    /// </summary>
    public required int Ordinal { get; init; }

    /// <summary>
    /// Gets the canonical policy name groups for this expression.
    /// </summary>
    public required ImmutableArray<ImmutableArray<string>> Groups { get; init; }

    /// <summary>
    /// Gets the canonical text representation of the expression.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Formats the policy expression.
    /// </summary>
    public string Format() => Text;

    /// <inheritdoc />
    public bool Equals(PolicyConditionExpression? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && Ordinal == other.Ordinal
                && Groups.Length == other.Groups.Length
                && Groups.Zip(other.Groups).All(pair =>
                    pair.First.SequenceEqual(pair.Second, StringComparer.Ordinal)));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Ordinal);
        foreach (var group in Groups)
        {
            foreach (var name in group)
            {
                hash.Add(name, StringComparer.Ordinal);
            }

            hash.Add(group.Length);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Describes one application of a policy expression within a policy gate.
/// </summary>
public sealed record PolicyConditionApplication
{
    /// <summary>
    /// Gets the ordinal of the referenced policy expression.
    /// </summary>
    public required int ExpressionOrdinal { get; init; }

    /// <summary>
    /// Gets the denial behavior declared by the application.
    /// </summary>
    public required PolicyDenialBehavior OnDenied { get; init; }
}

/// <summary>
/// Identifies one policy-bearing occurrence in a compiled plan operation.
/// </summary>
public readonly record struct PolicyOccurrenceReference
{
    /// <summary>
    /// Gets the plan part, where zero is the root and positive values identify incremental plans.
    /// </summary>
    public required int PlanPart { get; init; }

    /// <summary>
    /// Gets the compiled selection-set identifier.
    /// </summary>
    public required int SelectionSetId { get; init; }

    /// <summary>
    /// Gets the compiled selection identifier, or minus one for an object occurrence.
    /// </summary>
    public required int SelectionId { get; init; }

    /// <summary>
    /// Gets the zero-based ordinal among occurrences of the compiled selection.
    /// </summary>
    public required int OccurrenceOrdinal { get; init; }

    /// <summary>
    /// Gets the declaration-order ordinal of the policy application on the compiled coordinate.
    /// </summary>
    public required int ApplicationOrdinal { get; init; }

    /// <summary>
    /// Gets the semantic evaluation facet claimed for the application occurrence.
    /// </summary>
    public required PolicyOccurrenceFacet Facet { get; init; }
}

/// <summary>
/// Identifies the required semantic evaluation facet of a compiled policy application occurrence.
/// </summary>
public enum PolicyOccurrenceFacet
{
    /// <summary>The request-time slot evaluation facet.</summary>
    SlotGate,

    /// <summary>The result-time residual evaluation facet.</summary>
    ResidualEvaluation
}

/// <summary>
/// Describes one coordinate controlled by a policy condition slot.
/// </summary>
public sealed record PolicyConditionCoordinate
{
    /// <summary>
    /// Gets the compiled occurrences controlled by this coordinate.
    /// </summary>
    public ImmutableArray<PolicyOccurrenceReference> Occurrences { get; init; } = [];

    /// <summary>
    /// Gets the composite type that owns the coordinate.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets the schema field name for a field coordinate, or <c>null</c> for an object coordinate.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Gets the response names that materialize this field coordinate.
    /// </summary>
    public required ImmutableArray<string> ResponseNames { get; init; }

    /// <summary>
    /// Gets the expression applications in declaration order for this coordinate.
    /// </summary>
    public required ImmutableArray<PolicyConditionApplication> Applications { get; init; }

    /// <summary>
    /// Gets whether this coordinate is the operation root object.
    /// </summary>
    public required bool IsRoot { get; init; }

    /// <summary>
    /// Gets the client include-condition masks that can make this coordinate live.
    /// </summary>
    public required ImmutableArray<ConditionFlags> LiveGuardMasks { get; init; }

    /// <summary>
    /// Gets the client include-condition masks that permit a denied decision to gate source fetches.
    /// </summary>
    public required ImmutableArray<ConditionFlags> GateGuardMasks { get; init; }

    /// <summary>
    /// Gets the per-event resource requirements a subscription-root coordinate reads from the
    /// composed <c>@eventStream</c> message projection instead of a residual policy execution
    /// node. Empty for every other coordinate.
    /// </summary>
    public ImmutableArray<PolicyRequirement> Requirements { get; init; } = [];

    /// <inheritdoc />
    public bool Equals(PolicyConditionCoordinate? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && TypeName.Equals(other.TypeName, StringComparison.Ordinal)
                && string.Equals(FieldName, other.FieldName, StringComparison.Ordinal)
                && IsRoot == other.IsRoot
                && Occurrences.SequenceEqual(other.Occurrences)
                && ResponseNames.SequenceEqual(other.ResponseNames, StringComparer.Ordinal)
                && Applications.SequenceEqual(other.Applications)
                && PolicyGuardMasks.SequenceEqual(LiveGuardMasks, other.LiveGuardMasks)
                && PolicyGuardMasks.SequenceEqual(GateGuardMasks, other.GateGuardMasks)
                && RequirementsEqual(Requirements, other.Requirements));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TypeName, StringComparer.Ordinal);
        hash.Add(FieldName, StringComparer.Ordinal);
        hash.Add(IsRoot);
        foreach (var occurrence in Occurrences)
        {
            hash.Add(occurrence);
        }
        foreach (var responseName in ResponseNames)
        {
            hash.Add(responseName, StringComparer.Ordinal);
        }

        foreach (var application in Applications)
        {
            hash.Add(application);
        }

        foreach (var guardMask in LiveGuardMasks)
        {
            PolicyGuardMasks.AddHashCode(ref hash, guardMask);
        }

        foreach (var guardMask in GateGuardMasks)
        {
            PolicyGuardMasks.AddHashCode(ref hash, guardMask);
        }

        foreach (var requirement in Requirements)
        {
            hash.Add(requirement.PolicyName, StringComparer.Ordinal);
            hash.Add(SyntaxComparer.BySyntax.GetHashCode(requirement.SelectionSet));
        }

        return hash.ToHashCode();
    }

    private static bool RequirementsEqual(
        ImmutableArray<PolicyRequirement> left,
        ImmutableArray<PolicyRequirement> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].PolicyName.Equals(right[i].PolicyName, StringComparison.Ordinal)
                || !SyntaxComparer.BySyntax.Equals(left[i].SelectionSet, right[i].SelectionSet))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Describes one plan-local boolean gate for a policy-protected coordinate.
/// </summary>
public sealed record PolicyConditionSlot
{
    /// <summary>
    /// Gets the zero-based, plan-local gate ordinal.
    /// </summary>
    public required int Ordinal { get; init; }

    /// <summary>
    /// Gets the expression applications whose request-cacheable decisions contribute to this gate.
    /// </summary>
    public required ImmutableArray<PolicyConditionApplication> Applications { get; init; }

    /// <summary>
    /// Gets the greatest denial behavior among applications that remain on the execution-node path.
    /// </summary>
    public required PolicyDenialBehavior Rmax { get; init; }

    /// <summary>
    /// Gets the canonical DNF masks of client include conditions that can make this gate live.
    /// </summary>
    public required ImmutableArray<ConditionFlags> GuardMasks { get; init; }

    /// <summary>
    /// Gets the operation coordinates controlled by this gate.
    /// </summary>
    public required ImmutableArray<PolicyConditionCoordinate> Coordinates { get; init; }

    /// <summary>
    /// Gets the reserved variable name that carries the gated allowed value.
    /// </summary>
    public string VariableName => $"__fusion_policy_{Ordinal}";

    /// <inheritdoc />
    public bool Equals(PolicyConditionSlot? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && Ordinal == other.Ordinal
                && Rmax == other.Rmax
                && Applications.SequenceEqual(other.Applications)
                && PolicyGuardMasks.SequenceEqual(GuardMasks, other.GuardMasks)
                && Coordinates.SequenceEqual(other.Coordinates));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Ordinal);
        hash.Add(Rmax);
        foreach (var application in Applications)
        {
            hash.Add(application);
        }

        foreach (var guardMask in GuardMasks)
        {
            PolicyGuardMasks.AddHashCode(ref hash, guardMask);
        }

        foreach (var coordinate in Coordinates)
        {
            hash.Add(coordinate);
        }

        return hash.ToHashCode();
    }
}

internal static class PolicyGuardMasks
{
    public static bool Equals(ConditionFlags left, ConditionFlags right)
    {
        if (left.Word0 != right.Word0)
        {
            return false;
        }

        var leftLength = GetOverflowLength(left.Overflow);
        var rightLength = GetOverflowLength(right.Overflow);
        return leftLength == rightLength
            && left.Overflow.AsSpan(0, leftLength).SequenceEqual(
                right.Overflow.AsSpan(0, rightLength));
    }

    public static int Compare(ConditionFlags left, ConditionFlags right)
    {
        var comparison = left.Word0.CompareTo(right.Word0);
        if (comparison != 0)
        {
            return comparison;
        }

        var leftLength = GetOverflowLength(left.Overflow);
        var rightLength = GetOverflowLength(right.Overflow);
        var length = Math.Min(leftLength, rightLength);

        for (var i = 0; i < length; i++)
        {
            comparison = left.Overflow![i].CompareTo(right.Overflow![i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return leftLength.CompareTo(rightLength);
    }

    public static bool SequenceEqual(ImmutableArray<ConditionFlags> left, ImmutableArray<ConditionFlags> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsSubsetOf(ConditionFlags subset, ConditionFlags superset)
    {
        if ((subset.Word0 & superset.Word0) != subset.Word0)
        {
            return false;
        }

        var subsetLength = GetOverflowLength(subset.Overflow);
        var supersetLength = superset.Overflow?.Length ?? 0;
        for (var i = 0; i < subsetLength; i++)
        {
            var subsetWord = subset.Overflow![i];
            var supersetWord = i < supersetLength ? superset.Overflow![i] : 0UL;
            if ((subsetWord & supersetWord) != subsetWord)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsEmpty(ConditionFlags mask)
        => mask.Word0 == 0 && GetOverflowLength(mask.Overflow) == 0;

    public static bool IsCanonical(ConditionFlags mask)
        => mask.Overflow is null
            || (mask.Overflow.Length > 0 && mask.Overflow[^1] != 0);

    public static bool Contains(ConditionFlags mask, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (index < 64)
        {
            return (mask.Word0 & (1UL << index)) != 0;
        }

        var overflowIndex = (index >> 6) - 1;
        return mask.Overflow is { } overflow
            && overflowIndex < overflow.Length
            && (overflow[overflowIndex] & (1UL << (index & 63))) != 0;
    }

    public static ConditionFlags FromIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (index < 64)
        {
            return new ConditionFlags(1UL << index);
        }

        var overflowIndex = (index >> 6) - 1;
        var overflow = new ulong[overflowIndex + 1];
        overflow[overflowIndex] = 1UL << (index & 63);
        return new ConditionFlags(0, overflow);
    }

    public static ConditionFlags CreateAllSet(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
        {
            return default;
        }

        var word0 = count >= 64
            ? ulong.MaxValue
            : (1UL << count) - 1;
        if (count <= 64)
        {
            return new ConditionFlags(word0);
        }

        var overflow = new ulong[(count - 1) >> 6];
        Array.Fill(overflow, ulong.MaxValue);
        var finalWordBitCount = count & 63;
        if (finalWordBitCount != 0)
        {
            overflow[^1] = (1UL << finalWordBitCount) - 1;
        }

        return new ConditionFlags(word0, overflow);
    }

    public static ConditionFlags Or(ConditionFlags left, ConditionFlags right)
    {
        var leftLength = GetOverflowLength(left.Overflow);
        var rightLength = GetOverflowLength(right.Overflow);
        var length = Math.Max(leftLength, rightLength);
        if (length == 0)
        {
            return new ConditionFlags(left.Word0 | right.Word0);
        }

        var overflow = new ulong[length];
        for (var i = 0; i < length; i++)
        {
            var leftWord = i < leftLength ? left.Overflow![i] : 0UL;
            var rightWord = i < rightLength ? right.Overflow![i] : 0UL;
            overflow[i] = leftWord | rightWord;
        }

        return new ConditionFlags(left.Word0 | right.Word0, overflow);
    }

    public static ConditionFlags Intersect(ConditionFlags left, ConditionFlags right)
    {
        var length = Math.Min(
            GetOverflowLength(left.Overflow),
            GetOverflowLength(right.Overflow));
        if (length == 0)
        {
            return new ConditionFlags(left.Word0 & right.Word0);
        }

        var overflow = new ulong[length];
        for (var i = 0; i < length; i++)
        {
            overflow[i] = left.Overflow![i] & right.Overflow![i];
        }

        return Normalize(new ConditionFlags(left.Word0 & right.Word0, overflow));
    }

    public static ConditionFlags Normalize(ConditionFlags mask)
    {
        var length = GetOverflowLength(mask.Overflow);
        if (length == 0)
        {
            return new ConditionFlags(mask.Word0);
        }

        if (length == mask.Overflow!.Length)
        {
            return mask;
        }

        return new ConditionFlags(mask.Word0, mask.Overflow.AsSpan(0, length).ToArray());
    }

    public static ImmutableArray<ConditionFlags> Canonicalize(IEnumerable<ConditionFlags> masks)
    {
        var ordered = masks.Select(Normalize).ToArray();
        Array.Sort(ordered, Compare);

        if (ordered.Length == 0)
        {
            return [];
        }

        if (IsEmpty(ordered[0]))
        {
            return [default];
        }

        var canonical = ImmutableArray.CreateBuilder<ConditionFlags>(ordered.Length);
        foreach (var mask in ordered)
        {
            if ((canonical.Count > 0 && Equals(mask, canonical[^1]))
                || canonical.Any(existing => IsSubsetOf(existing, mask)))
            {
                continue;
            }

            canonical.Add(mask);
        }

        return canonical.ToImmutable();
    }

    public static void AddHashCode(ref HashCode hash, ConditionFlags mask)
    {
        hash.Add(mask.Word0);
        var length = GetOverflowLength(mask.Overflow);
        hash.Add(length);
        for (var i = 0; i < length; i++)
        {
            hash.Add(mask.Overflow![i]);
        }
    }

    private static int GetOverflowLength(ulong[]? overflow)
    {
        var length = overflow?.Length ?? 0;
        while (length > 0 && overflow![length - 1] == 0)
        {
            length--;
        }

        return length;
    }
}
