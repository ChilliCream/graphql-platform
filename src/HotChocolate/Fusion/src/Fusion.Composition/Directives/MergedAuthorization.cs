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
    public bool IsEmpty => !Authenticated && Scopes.IsEmpty && Policies.IsEmpty;
}
