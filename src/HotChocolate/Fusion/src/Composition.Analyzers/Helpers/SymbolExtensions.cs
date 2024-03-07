using Microsoft.CodeAnalysis;

namespace HotChocolate.Fusion.Composition.Analyzers.Helpers;

public static class SymbolExtensions
{
    public static string ToFullyQualified(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
