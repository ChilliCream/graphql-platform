using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public interface ILocalTypeLookup
{
    bool TryGetTypeName(
        ITypeSymbol type,
        IMethodSymbol resolverMethod,
        [NotNullWhen(true)] out string? typeDisplayName);
}
