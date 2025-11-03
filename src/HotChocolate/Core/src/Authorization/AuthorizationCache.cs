using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Caching.Memory;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationCache(int capacity = 256)
{
    private readonly Cache<ImmutableArray<AuthorizeDirective>> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public bool TryGetDirectives(
        string documentId,
        [NotNullWhen(true)] out ImmutableArray<AuthorizeDirective>? directives)
    {
        if (_cache.TryGet(documentId, out var cachedDirectives))
        {
            directives = cachedDirectives;
            return true;
        }

        directives = null;
        return false;
    }

    public ImmutableArray<AuthorizeDirective> GetOrCreate<TState>(
        string documentId,
        Func<string, TState, ImmutableArray<AuthorizeDirective>> create,
        TState state)
        => _cache.GetOrCreate(documentId, create, state);
}
