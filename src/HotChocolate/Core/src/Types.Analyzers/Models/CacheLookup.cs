using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public readonly struct CacheLookup(IMethodSymbol method, bool isTransform = false)
{
    public IMethodSymbol Method { get; } = method;

    public bool IsTransform { get; } = isTransform;
}
