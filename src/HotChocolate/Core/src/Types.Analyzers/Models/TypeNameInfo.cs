using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed record TypeNameInfo
{
    public required string FullName { get; init; }

    public required string FullyQualifiedName { get; init; }

    public static TypeNameInfo Create(INamedTypeSymbol namedTypeSymbol)
    {
        return new TypeNameInfo
        {
            FullName = namedTypeSymbol.ToDisplayString(),
            FullyQualifiedName = namedTypeSymbol.ToFullyQualified()
        };
    }

    private TypeNameInfo() { }
}
