namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Provides the policies of the <c>@authenticated</c> and <c>@requiresScopes</c> directives.
/// It ignores the policy name and answers nothing for other directives.
/// </summary>
public sealed class BuiltInPolicyProvider : IPolicyProvider
{
    private readonly RequiresScopesPolicy _requiresScopes;

    /// <summary>
    /// Initializes a new instance of <see cref="BuiltInPolicyProvider"/> that reads scopes
    /// from the space delimited <c>scope</c> claim.
    /// </summary>
    public BuiltInPolicyProvider() : this(new RequiresScopesPolicy())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BuiltInPolicyProvider"/>.
    /// </summary>
    /// <param name="requiresScopes">
    /// The policy that evaluates <c>@requiresScopes</c>.
    /// </param>
    public BuiltInPolicyProvider(RequiresScopesPolicy requiresScopes)
    {
        ArgumentNullException.ThrowIfNull(requiresScopes);
        _requiresScopes = requiresScopes;
    }

    /// <inheritdoc />
    public IPolicy? GetPolicy(string policyName, string directiveName)
    {
        ArgumentNullException.ThrowIfNull(policyName);
        ArgumentNullException.ThrowIfNull(directiveName);

        return directiveName switch
        {
            PolicyDirectiveNames.Authenticated => AuthenticatedPolicy.Instance,
            PolicyDirectiveNames.RequiresScopes => _requiresScopes,
            _ => null
        };
    }
}
