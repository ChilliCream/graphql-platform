using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers;

public static class KnownSymbols
{
    public static INamedTypeSymbol GetConnectionBaseSymbol(this GeneratorSyntaxContext context)
        => context.SemanticModel.Compilation.GetConnectionBaseSymbol();

    public static INamedTypeSymbol GetConnectionBaseSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("HotChocolate.Types.Pagination.ConnectionBase`3")
            ?? throw new InvalidOperationException("Could not resolve connection base type.");

    public static INamedTypeSymbol GetTaskSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")
            ?? throw new InvalidOperationException("Could not resolve connection base type.");

    public static INamedTypeSymbol GetValueTaskSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")
            ?? throw new InvalidOperationException("Could not resolve connection base type.");

    public static bool IsConnectionBase(this GeneratorSyntaxContext context, ITypeSymbol possibleConnectionType)
        => context.SemanticModel.Compilation.IsConnectionBase(possibleConnectionType);

    public static bool IsTaskOrValueTask(
        this Compilation compilation,
        ITypeSymbol possibleTask,
        [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        if (possibleTask is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var taskSymbol = compilation.GetTaskSymbol();
            var valueTaskSymbol = compilation.GetValueTaskSymbol();

            if (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, taskSymbol) ||
                SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, valueTaskSymbol))
            {
                innerType = namedType.TypeArguments[0];
                return true;
            }
        }

        innerType = null;
        return false;
    }

    public static bool IsConnectionBase(this Compilation compilation, ITypeSymbol possibleConnectionType)
    {
        if (compilation.IsTaskOrValueTask(possibleConnectionType, out var innerType))
        {
            possibleConnectionType = innerType;
        }

        if (possibleConnectionType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return IsDerivedFromGenericBase(namedType, compilation.GetConnectionBaseSymbol());
    }

    private static bool IsDerivedFromGenericBase(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseTypeSymbol)
    {
        var current = typeSymbol;

        while (current is not null)
        {
            if (current is { IsGenericType: true } namedTypeSymbol)
            {
                var baseType = namedTypeSymbol.ConstructedFrom;
                if (SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, baseTypeSymbol))
                {
                    return true;
                }
            }

            current = current.BaseType;
        }

        return false;
    }
}
