using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents one policy application on a Fusion object or field coordinate.
/// The policy expression is a disjunction of policy name groups: names within
/// a group combine with AND, groups combine with OR.
/// </summary>
public sealed record PolicyApplication
{
    /// <summary>
    /// Gets the policy name groups that form the policy expression.
    /// </summary>
    public required ImmutableArray<ImmutableArray<string>> Groups { get; init; }

    /// <summary>
    /// Gets the behavior used when this policy application denies an entity.
    /// </summary>
    public required PolicyDenialBehavior OnDenied { get; init; }

    /// <summary>
    /// Formats the policy expression for diagnostics, for example (a AND b) OR c.
    /// </summary>
    public string Format() => PolicyNameGroups.Format(Groups);

    /// <summary>
    /// Compares this policy application to another by value, including the names
    /// within the policy name groups.
    /// </summary>
    public bool Equals(PolicyApplication? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (OnDenied != other.OnDenied || Groups.IsDefault != other.Groups.IsDefault)
        {
            return false;
        }

        if (Groups.IsDefault)
        {
            return true;
        }

        if (Groups.Length != other.Groups.Length)
        {
            return false;
        }

        for (var i = 0; i < Groups.Length; i++)
        {
            if (!Groups[i].AsSpan().SequenceEqual(other.Groups[i].AsSpan()))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(OnDenied);

        if (!Groups.IsDefault)
        {
            foreach (var group in Groups)
            {
                hashCode.Add(group.Length);

                foreach (var name in group)
                {
                    hashCode.Add(name, StringComparer.Ordinal);
                }
            }
        }

        return hashCode.ToHashCode();
    }
}
