using System.Collections.Immutable;

namespace HotChocolate.Fusion.Directives;

/// <summary>
/// An authorization requirement expressed as alternative groups of names.
/// </summary>
internal sealed class PolicyRequirement : IEquatable<PolicyRequirement>
{
    public static PolicyRequirement Empty { get; } = new([]);

    private PolicyRequirement(ImmutableArray<ImmutableArray<string>> groups)
    {
        Groups = groups;
    }

    /// <summary>
    /// Gets the alternative groups in canonical order.
    /// </summary>
    public ImmutableArray<ImmutableArray<string>> Groups { get; }

    /// <summary>
    /// Gets a value indicating whether this requirement has no groups.
    /// </summary>
    public bool IsEmpty => Groups.IsEmpty;

    /// <summary>
    /// Creates a requirement with canonicalized groups.
    /// </summary>
    public static PolicyRequirement Create(IEnumerable<ImmutableArray<string>> groups)
    {
        var normalized = groups.Select(Normalize).ToList();
        var minimized = Minimize(normalized);
        return minimized.IsEmpty ? Empty : new PolicyRequirement(minimized);
    }

    /// <summary>
    /// Combines this requirement with <paramref name="other"/> so that both must be satisfied.
    /// </summary>
    public PolicyRequirement And(PolicyRequirement other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        var combined = new List<ImmutableArray<string>>(Groups.Length * other.Groups.Length);

        foreach (var left in Groups)
        {
            foreach (var right in other.Groups)
            {
                combined.Add(left.AddRange(right));
            }
        }

        return Create(combined);
    }

    /// <summary>
    /// Determines whether this requirement covers <paramref name="dependency"/>.
    /// </summary>
    public bool Covers(PolicyRequirement dependency)
    {
        if (dependency.IsEmpty)
        {
            return true;
        }

        if (IsEmpty)
        {
            return false;
        }

        foreach (var group in Groups)
        {
            var covered = false;

            foreach (var dependencyGroup in dependency.Groups)
            {
                if (IsSubset(dependencyGroup, group))
                {
                    covered = true;
                    break;
                }
            }

            if (!covered)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool Equals(PolicyRequirement? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || Groups.Length != other.Groups.Length)
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

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PolicyRequirement other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var group in Groups)
        {
            foreach (var value in group)
            {
                hash.Add(value, StringComparer.Ordinal);
            }

            hash.Add(group.Length);
        }

        return hash.ToHashCode();
    }

    private static ImmutableArray<string> Normalize(ImmutableArray<string> group)
    {
        var set = new SortedSet<string>(group, StringComparer.Ordinal);
        return [.. set];
    }

    private static ImmutableArray<ImmutableArray<string>> Minimize(List<ImmutableArray<string>> groups)
    {
        groups.Sort(CompareGroups);

        var kept = new List<ImmutableArray<string>>(groups.Count);

        foreach (var candidate in groups)
        {
            var redundant = false;

            foreach (var existing in kept)
            {
                if (IsSubset(existing, candidate))
                {
                    redundant = true;
                    break;
                }
            }

            if (!redundant)
            {
                kept.Add(candidate);
            }
        }

        return [.. kept];
    }

    private static bool IsSubset(ImmutableArray<string> subset, ImmutableArray<string> superset)
    {
        if (subset.Length > superset.Length)
        {
            return false;
        }

        var j = 0;

        foreach (var value in subset)
        {
            while (j < superset.Length
                && string.CompareOrdinal(superset[j], value) < 0)
            {
                j++;
            }

            if (j == superset.Length || !string.Equals(superset[j], value, StringComparison.Ordinal))
            {
                return false;
            }

            j++;
        }

        return true;
    }

    private static int CompareGroups(ImmutableArray<string> x, ImmutableArray<string> y)
    {
        if (x.Length != y.Length)
        {
            return x.Length.CompareTo(y.Length);
        }

        for (var i = 0; i < x.Length; i++)
        {
            var result = string.CompareOrdinal(x[i], y[i]);

            if (result != 0)
            {
                return result;
            }
        }

        return 0;
    }
}
