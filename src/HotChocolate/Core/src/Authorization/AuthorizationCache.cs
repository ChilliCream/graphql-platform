using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationCache
{
    private readonly Cache<AuthorizeDirective[]> _cache;

    public AuthorizationCache(int capacity = 100)
    {
        _cache = new Cache<AuthorizeDirective[]>(capacity);
    }

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Usage;

    public bool TryGetDirectives(
        string documentId,
        [NotNullWhen(true)] out AuthorizeDirective[]? directives)
        => _cache.TryGet(documentId, out directives);

    public void TryAddDirectives(string documentId, AuthorizeDirective[] directives)
        => _cache.GetOrCreate(documentId, () => directives);

    public void Clear()
        => _cache.Clear();
}
