namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Creates the authorization policies owned by a Fusion schema.
/// </summary>
/// <remarks>
/// The provider is a schema service and is invoked once while the schema is created.
/// The returned policies are not services. The provider owns their lifetime and must
/// dispose them when the provider is disposed with the schema services. Policies must
/// be safe for concurrent evaluation.
/// </remarks>
public interface IAuthorizationPolicyProvider
{
    /// <summary>
    /// Creates the authorization policies for a Fusion schema.
    /// </summary>
    /// <returns>
    /// The authorization policies owned by this provider.
    /// </returns>
    IReadOnlyList<IAuthorizationPolicy> CreatePolicies();
}
