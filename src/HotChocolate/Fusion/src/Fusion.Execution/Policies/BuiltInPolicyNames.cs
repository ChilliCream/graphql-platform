namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Names of the policies the gateway provides by default; see <see cref="CompositePolicyProvider"/>.
/// </summary>
internal static class BuiltInPolicyNames
{
    /// <summary>
    /// The built-in policy that allows an authenticated request.
    /// </summary>
    public const string Authenticated = "fusion.authenticated";

    /// <summary>
    /// The prefix of the built-in policy that allows a request whose user carries a given scope.
    /// The full policy name is this prefix followed by the scope, for example
    /// <c>fusion.scope:read:users</c>.
    /// </summary>
    public const string ScopePrefix = "fusion.scope:";
}
