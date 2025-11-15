using System.Collections.Concurrent;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class AuthorizationPolicyCache
{
    private readonly ConcurrentDictionary<AuthorizeDirective, AuthorizationPolicy> _cache = new();

    public AuthorizationPolicy? LookupPolicy(AuthorizeDirective directive)
    {
        return _cache.GetValueOrDefault(directive);
    }

    public void CachePolicy(AuthorizeDirective directive, AuthorizationPolicy policy)
    {
        _cache.TryAdd(directive, policy);
    }
}
