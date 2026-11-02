using System.Collections.Immutable;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The cumulative condition under which a selection is active: the possible
/// runtime object types it can apply to, intersected with every enclosing
/// type condition, and a canonical conjunction of <c>@include</c>/<c>@skip</c>
/// Boolean literals.
/// </summary>
internal readonly struct Condition : IEquatable<Condition>
{
    public Condition(PossibleTypeSet possibleTypes, ImmutableArray<BooleanLiteral> booleanCondition)
    {
        PossibleTypes = possibleTypes;
        BooleanCondition = booleanCondition;
    }

    /// <summary>
    /// Gets the set of object types this condition is active under.
    /// </summary>
    public PossibleTypeSet PossibleTypes { get; }

    /// <summary>
    /// Gets the canonical conjunction of Boolean literals this condition
    /// requires: sorted by variable name, at most one literal per variable.
    /// </summary>
    public ImmutableArray<BooleanLiteral> BooleanCondition { get; }

    /// <inheritdoc />
    public bool Equals(Condition other)
        => PossibleTypes.Equals(other.PossibleTypes)
            && BooleanCondition.AsSpan().SequenceEqual(other.BooleanCondition.AsSpan());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Condition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PossibleTypes);

        foreach (var literal in BooleanCondition)
        {
            hash.Add(literal);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Inserts one Boolean literal into an already canonical conjunction.
    /// Returns <see langword="false"/> when the literal contradicts one
    /// already present; when it duplicates one already present, the
    /// returned conjunction is unchanged.
    /// </summary>
    public static bool TryInsertLiteral(
        ImmutableArray<BooleanLiteral> canonical,
        BooleanLiteral literal,
        out ImmutableArray<BooleanLiteral> result)
    {
        var insertAt = canonical.Length;

        for (var i = 0; i < canonical.Length; i++)
        {
            var comparison = string.CompareOrdinal(canonical[i].VariableName, literal.VariableName);

            if (comparison == 0)
            {
                if (canonical[i].IsPositive != literal.IsPositive)
                {
                    result = default;
                    return false;
                }

                result = canonical;
                return true;
            }

            if (comparison > 0)
            {
                insertAt = i;
                break;
            }
        }

        result = canonical.Insert(insertAt, literal);
        return true;
    }
}
