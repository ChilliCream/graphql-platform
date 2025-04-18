using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationCache(int capacity = 256)
{
    private readonly Cache<AuthorizeDirective[]> _cache = new(capacity);

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Count;

    public bool TryGetDirectives(string documentId, [NotNullWhen(true)] out AuthorizeDirective[]? directives)
        => _cache.TryGet(documentId, out directives);

    public void TryAddDirectives(string documentId, AuthorizeDirective[] directives)
        => _cache.GetOrCreate(documentId, static (_, d) => d, directives);

    public void Clear()
        => _cache.Clear();
}
