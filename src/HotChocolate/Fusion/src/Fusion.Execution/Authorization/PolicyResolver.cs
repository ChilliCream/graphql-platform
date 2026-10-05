namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// Resolves policies through the built-in provider first and then through the registered
/// providers in registration order. The first provider that knows the policy wins.
/// </summary>
public sealed class PolicyResolver : IPolicyResolver
{
    private readonly IPolicyProvider[] _providers;

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyResolver"/>.
    /// </summary>
    /// <param name="builtInProvider">
    /// The provider that is always asked first.
    /// </param>
    /// <param name="providers">
    /// The providers in the order they are asked after the built-in provider.
    /// </param>
    public PolicyResolver(
        IPolicyProvider builtInProvider,
        IEnumerable<IPolicyProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(builtInProvider);
        ArgumentNullException.ThrowIfNull(providers);

        _providers = [builtInProvider, .. providers];
    }

    /// <inheritdoc />
    public IPolicy? Resolve(string policyName, string directiveName)
    {
        ArgumentNullException.ThrowIfNull(policyName);
        ArgumentNullException.ThrowIfNull(directiveName);

        foreach (var provider in _providers)
        {
            var policy = provider.GetPolicy(policyName, directiveName);

            if (policy is not null)
            {
                return policy;
            }
        }

        return null;
    }
}
