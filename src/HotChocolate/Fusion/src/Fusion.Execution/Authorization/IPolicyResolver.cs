namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Resolves the <see cref="IPolicy"/> for a policy name across all registered providers.
/// </summary>
public interface IPolicyResolver
{
    /// <summary>
    /// Resolves the policy for the given policy name and directive.
    /// </summary>
    /// <param name="policyName">
    /// The opaque policy name.
    /// </param>
    /// <param name="directiveName">
    /// The name of the directive that references the policy.
    /// </param>
    /// <returns>
    /// The policy of the first provider that knows it, or <c>null</c> if no provider does.
    /// </returns>
    IPolicy? Resolve(string policyName, string directiveName);
}
