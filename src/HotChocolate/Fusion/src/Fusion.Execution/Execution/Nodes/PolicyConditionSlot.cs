using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

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
    public required ImmutableArray<ulong> LiveGuardMasks { get; init; }

    /// <summary>
    /// Gets the client include-condition masks that permit a denied decision to gate source fetches.
    /// </summary>
    public required ImmutableArray<ulong> GateGuardMasks { get; init; }

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
                && LiveGuardMasks.SequenceEqual(other.LiveGuardMasks)
                && GateGuardMasks.SequenceEqual(other.GateGuardMasks));

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
            hash.Add(guardMask);
        }

        foreach (var guardMask in GateGuardMasks)
        {
            hash.Add(guardMask);
        }

        return hash.ToHashCode();
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
    public required ImmutableArray<ulong> GuardMasks { get; init; }

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
                && GuardMasks.SequenceEqual(other.GuardMasks)
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
            hash.Add(guardMask);
        }

        foreach (var coordinate in Coordinates)
        {
            hash.Add(coordinate);
        }

        return hash.ToHashCode();
    }
}
