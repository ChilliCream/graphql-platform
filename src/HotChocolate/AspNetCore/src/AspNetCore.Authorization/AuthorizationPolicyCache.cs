using System.Collections.Concurrent;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class AuthorizationPolicyCache(IAuthorizationPolicyProvider policyProvider)
{
    private readonly ConcurrentDictionary<string, Task<AuthorizationPolicy>> _cache = new();

    public Task<AuthorizationPolicy> GetOrCreatePolicyAsync(AuthorizeDirective directive)
    {
        var cacheKey = directive.GetPolicyCacheKey();

        return _cache.GetOrAdd(cacheKey, _ => BuildAuthorizationPolicy(directive.Policy, directive.Roles));
    }

    private async Task<AuthorizationPolicy> BuildAuthorizationPolicy(
        string? policyName,
        IReadOnlyList<string>? roles)
    {
        var policyBuilder = new AuthorizationPolicyBuilder();

        if (!string.IsNullOrWhiteSpace(policyName))
        {
            var policy = await policyProvider.GetPolicyAsync(policyName).ConfigureAwait(false);

            if (policy is not null)
            {
                policyBuilder = policyBuilder.Combine(policy);
            }
            else
            {
                throw new MissingAuthorizationPolicyException(policyName);
            }
        }
        else
        {
            var defaultPolicy = await policyProvider.GetDefaultPolicyAsync().ConfigureAwait(false);

            policyBuilder = policyBuilder.Combine(defaultPolicy);
        }

        if (roles is not null)
        {
            policyBuilder = policyBuilder.RequireRole(roles);
        }

        return policyBuilder.Build();
    }
}
