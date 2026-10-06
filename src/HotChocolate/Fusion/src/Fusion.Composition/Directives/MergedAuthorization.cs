using System.Collections.Immutable;

namespace HotChocolate.Fusion.Directives;

/// <summary>
/// The merged authorization requirement of a type system member across source schemas.
/// </summary>
/// <param name="Authenticated">
/// <c>true</c> if any source schema marks the member with <c>@authenticated</c>.
/// </param>
/// <param name="Scopes">
/// The reduced scope groups in canonical order. A group is a set of scopes that must all be
/// present, and one satisfied group is sufficient. Empty if no source schema requires scopes.
/// </param>
/// <param name="Policies">
/// The reduced policy groups in canonical order, with the same semantics as the scope groups.
/// </param>
internal sealed record MergedAuthorization(
    bool Authenticated,
    ImmutableArray<ImmutableArray<string>> Scopes,
    ImmutableArray<ImmutableArray<string>> Policies)
{
    public static MergedAuthorization Empty { get; } = new(false, [], []);

    public bool IsEmpty => !Authenticated && Scopes.IsEmpty && Policies.IsEmpty;

    /// <summary>
    /// Combines this requirement with <paramref name="other"/> so that both must be satisfied.
    /// </summary>
    public MergedAuthorization And(MergedAuthorization other)
    {
        if (other.IsEmpty)
        {
            return this;
        }

        if (IsEmpty)
        {
            return other;
        }

        return new MergedAuthorization(
            Authenticated || other.Authenticated,
            CombineGroups(Scopes, other.Scopes),
            CombineGroups(Policies, other.Policies));
    }

    private static ImmutableArray<ImmutableArray<string>> CombineGroups(
        ImmutableArray<ImmutableArray<string>> left,
        ImmutableArray<ImmutableArray<string>> right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        return right.IsEmpty ? left : AuthorizationGroups.Reduce([left, right]);
    }

    /// <summary>
    /// Determines whether <paramref name="other"/> carries the same requirement. Both
    /// requirements are expected in canonical form.
    /// </summary>
    public bool Matches(MergedAuthorization other)
    {
        return Authenticated == other.Authenticated
            && GroupsMatch(Scopes, other.Scopes)
            && GroupsMatch(Policies, other.Policies);
    }

    private static bool GroupsMatch(
        ImmutableArray<ImmutableArray<string>> left,
        ImmutableArray<ImmutableArray<string>> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].AsSpan().SequenceEqual(right[i].AsSpan()))
            {
                return false;
            }
        }

        return true;
    }
}
