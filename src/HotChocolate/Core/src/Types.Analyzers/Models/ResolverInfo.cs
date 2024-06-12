using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public readonly struct ResolverInfo(ResolverName resolverName, IMethodSymbol? methodSymbol)
{
    public readonly ResolverName Name = resolverName;

    public readonly IMethodSymbol? Method = methodSymbol;

    public readonly int ParameterCount = methodSymbol?.Parameters.Length ?? 0;

    public bool Skip =>
        ParameterCount == 0 ||
        (ParameterCount == 1 && (Method?.Parameters[0].IsParent() ?? false));
}
