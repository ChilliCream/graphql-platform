using System.Collections.Immutable;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Directives;

/// <summary>
/// The merged policy requirement of a type system member across source schemas.
/// </summary>
/// <param name="Authenticated">
/// <c>true</c> if any source schema marks the member with <c>@authenticated</c>.
/// </param>
/// <param name="Scopes">
/// The reduced scope requirement. A group is a set of scopes that must all be present, and one
/// satisfied group is sufficient.
/// </param>
/// <param name="Policies">
/// The reduced policy requirement, with the same semantics as the scope requirement.
/// </param>
internal sealed record MergedPolicy(
    bool Authenticated,
    PolicyRequirement Scopes,
    PolicyRequirement Policies)
{
    public static MergedPolicy Empty { get; } = new(false, PolicyRequirement.Empty, PolicyRequirement.Empty);

    public bool IsEmpty => !Authenticated && Scopes.IsEmpty && Policies.IsEmpty;

    /// <summary>
    /// Combines this requirement with <paramref name="other"/> so that both must be satisfied.
    /// </summary>
    public MergedPolicy And(MergedPolicy other)
    {
        if (other.IsEmpty)
        {
            return this;
        }

        if (IsEmpty)
        {
            return other;
        }

        return new MergedPolicy(
            Authenticated || other.Authenticated,
            Scopes.And(other.Scopes),
            Policies.And(other.Policies));
    }

    /// <summary>
    /// Determines whether <paramref name="other"/> carries the same requirement.
    /// </summary>
    public bool Matches(MergedPolicy other)
    {
        return Authenticated == other.Authenticated
            && Scopes.Equals(other.Scopes)
            && Policies.Equals(other.Policies);
    }

    /// <summary>
    /// Gets the names of the directives of <paramref name="dependency"/> that this requirement
    /// does not cover.
    /// </summary>
    public ImmutableArray<string> GetUncovered(MergedPolicy dependency)
    {
        var uncovered = ImmutableArray.CreateBuilder<string>();

        if (dependency.Authenticated && !Authenticated)
        {
            uncovered.Add(DirectiveNames.Authenticated);
        }

        if (!Scopes.Covers(dependency.Scopes))
        {
            uncovered.Add(DirectiveNames.RequiresScopes);
        }

        if (!Policies.Covers(dependency.Policies))
        {
            uncovered.Add(DirectiveNames.Policy);
        }

        return uncovered.ToImmutable();
    }
}
