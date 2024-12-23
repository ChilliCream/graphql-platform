using System.Collections.Concurrent;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class AuthorizationPolicyCache
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new();

    public AuthorizationPolicy? LookupPolicy(AuthorizeDirective directive)
    {
        var cacheKey = directive.GetPolicyCacheKey();

        return _cache.GetValueOrDefault(cacheKey);
    }

    public void CachePolicy(AuthorizeDirective directive, AuthorizationPolicy policy)
    {
        var cacheKey = directive.GetPolicyCacheKey();

        _cache.TryAdd(cacheKey, policy);
    }
}
