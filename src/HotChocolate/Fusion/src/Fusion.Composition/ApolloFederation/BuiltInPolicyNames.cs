namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Names of the built-in Fusion policies that Apollo's <c>@authenticated</c> and
/// <c>@requiresScopes</c> directives translate to. These names are evaluated at request time by
/// the gateway's built-in policy set; see the Fusion.Execution policy provider pipeline.
/// </summary>
internal static class BuiltInPolicyNames
{
    /// <summary>
    /// The policy name that requires the request to be authenticated. Translated from Apollo's
    /// <c>@authenticated</c> directive, and applied a second time (alongside the scope policy)
    /// for <c>@requiresScopes</c>, since a scope check implies authentication.
    /// </summary>
    public const string Authenticated = "fusion.authenticated";

    /// <summary>
    /// The prefix that turns an Apollo scope name (from <c>@requiresScopes</c>) into the name of
    /// the built-in Fusion policy that checks for that scope.
    /// </summary>
    public const string ScopePrefix = "fusion.scope:";
}
