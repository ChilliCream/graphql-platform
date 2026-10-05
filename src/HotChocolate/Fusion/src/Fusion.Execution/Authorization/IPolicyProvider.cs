namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Provides the <see cref="IPolicy"/> for a policy name.
/// </summary>
public interface IPolicyProvider
{
    /// <summary>
    /// Gets the policy for the given policy name and directive.
    /// </summary>
    /// <param name="policyName">
    /// The opaque policy name.
    /// </param>
    /// <param name="directiveName">
    /// The name of the directive that references the policy.
    /// </param>
    /// <returns>
    /// The policy, or <c>null</c> if this provider does not know the policy.
    /// </returns>
    IPolicy? GetPolicy(string policyName, string directiveName);
}
