namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// The names of the authorization directives.
/// </summary>
public static class PolicyDirectiveNames
{
    /// <summary>
    /// The name of the <c>@authenticated</c> directive.
    /// </summary>
    public const string Authenticated = "authenticated";

    /// <summary>
    /// The name of the <c>@requiresScopes</c> directive.
    /// </summary>
    public const string RequiresScopes = "requiresScopes";

    /// <summary>
    /// The name of the <c>@policy</c> directive.
    /// </summary>
    public const string Policy = "policy";

    /// <summary>
    /// Gets the position of the directive in the evaluation order: authenticated,
    /// then scopes, then named policies.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    public static int GetEvaluationOrder(string directiveName)
    {
        ArgumentNullException.ThrowIfNull(directiveName);

        return directiveName switch
        {
            Authenticated => 0,
            RequiresScopes => 1,
            _ => 2
        };
    }
}
