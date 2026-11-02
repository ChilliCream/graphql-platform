using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Directives;

/// <summary>
/// The merged authorization requirement of a type system member as written by composition.
/// The authenticated, scopes and policies parts are independent and all must be satisfied.
/// </summary>
internal sealed class AuthorizationDirective(
    bool authenticated,
    ImmutableArray<ImmutableArray<string>> scopes,
    ImmutableArray<ImmutableArray<string>> policies)
{
    /// <summary>
    /// Gets a value indicating whether the member requires an authenticated user.
    /// </summary>
    public bool Authenticated { get; } = authenticated;

    /// <summary>
    /// Gets the alternative scope groups in canonical order, or an empty array if no scopes are
    /// required. One fully granted group satisfies the requirement.
    /// </summary>
    public ImmutableArray<ImmutableArray<string>> Scopes { get; } = scopes;

    /// <summary>
    /// Gets the alternative policy groups in canonical order, or an empty array if no policies
    /// are required. One fully satisfied group satisfies the requirement.
    /// </summary>
    public ImmutableArray<ImmutableArray<string>> Policies { get; } = policies;
}
