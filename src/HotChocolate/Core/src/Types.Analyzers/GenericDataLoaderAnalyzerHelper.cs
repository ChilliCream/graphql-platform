using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

internal static class GenericDataLoaderAnalyzerHelper
{
    public static ImmutableArray<DataLoaderAttributeInfo> GetDataLoaderAttributes(
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var dataLoaderAttribute = compilation.GetTypeByMetadataName(
            WellKnownAttributes.DataLoaderAttribute);
        var genericDataLoaderAttribute = compilation.GetTypeByMetadataName(
            WellKnownAttributes.GenericDataLoaderAttribute);

        if (dataLoaderAttribute is null && genericDataLoaderAttribute is null)
        {
            return [];
        }

        var attributes = ImmutableArray.CreateBuilder<DataLoaderAttributeInfo>();

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                if (dataLoaderAttribute is not null
                    && SymbolEqualityComparer.Default.Equals(
                        attributeType.OriginalDefinition,
                        dataLoaderAttribute))
                {
                    attributes.Add(new DataLoaderAttributeInfo(attribute, attributeType, false));
                }
                else if (genericDataLoaderAttribute is not null
                    && SymbolEqualityComparer.Default.Equals(
                        attributeType.OriginalDefinition,
                        genericDataLoaderAttribute))
                {
                    attributes.Add(new DataLoaderAttributeInfo(attribute, attributeType, true));
                }
            }
        }

        return attributes.ToImmutable();
    }

    public static bool TryResolveContract(
        INamedTypeSymbol dataLoaderType,
        Compilation compilation,
        out DataLoaderContract contract)
    {
        var batchDataLoader = compilation.GetTypeByMetadataName("GreenDonut.IBatchDataLoader`2");
        var cacheDataLoader = compilation.GetTypeByMetadataName("GreenDonut.ICacheDataLoader`2");

        if (batchDataLoader is null
            || cacheDataLoader is null
            || dataLoaderType.TypeKind is not TypeKind.Interface
            || !IsClosed(dataLoaderType))
        {
            contract = default;
            return false;
        }

        var contracts = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        if (IsDataLoaderContract(dataLoaderType, batchDataLoader, cacheDataLoader))
        {
            contracts.Add(dataLoaderType);
        }

        foreach (var interfaceType in dataLoaderType.AllInterfaces)
        {
            if (IsDataLoaderContract(interfaceType, batchDataLoader, cacheDataLoader))
            {
                contracts.Add(interfaceType);
            }
        }

        if (contracts.Count != 1)
        {
            contract = default;
            return false;
        }

        var contractType = contracts[0];
        contract = new DataLoaderContract(
            SymbolEqualityComparer.Default.Equals(contractType.ConstructedFrom, batchDataLoader)
                ? DataLoaderKind.Batch
                : DataLoaderKind.Cache,
            contractType.TypeArguments[0],
            contractType.TypeArguments[1]);
        return true;
    }

    public static bool HasValidKeyParameter(
        IMethodSymbol methodSymbol,
        DataLoaderContract contract,
        Compilation compilation)
    {
        if (methodSymbol.Parameters.Length == 0)
        {
            return false;
        }

        var keyParameter = methodSymbol.Parameters[0];

        if (contract.Kind is DataLoaderKind.Cache)
        {
            return SymbolEqualityComparer.Default.Equals(keyParameter.Type, contract.KeyType);
        }

        var readOnlyList = compilation.GetTypeByMetadataName(
            "System.Collections.Generic.IReadOnlyList`1");

        return keyParameter.Type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            && readOnlyList is not null
            && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, readOnlyList)
            && SymbolEqualityComparer.Default.Equals(namedType.TypeArguments[0], contract.KeyType);
    }

    public static bool HasValidReturnType(
        IMethodSymbol methodSymbol,
        DataLoaderContract contract,
        Compilation compilation)
    {
        if (methodSymbol.ReturnsByRef
            || methodSymbol.ReturnsByRefReadonly
            || !TryGetAsyncResultType(methodSymbol.ReturnType, compilation, out var resultType))
        {
            return false;
        }

        if (contract.Kind is DataLoaderKind.Cache)
        {
            return SymbolEqualityComparer.Default.Equals(resultType, contract.ValueType);
        }

        var readOnlyDictionary = compilation.GetTypeByMetadataName(
            "System.Collections.Generic.IReadOnlyDictionary`2");
        var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");

        return resultType is INamedTypeSymbol { TypeArguments.Length: 2 } namedType
            && (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, readOnlyDictionary)
                || SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, dictionary))
            && SymbolEqualityComparer.Default.Equals(namedType.TypeArguments[0], contract.KeyType)
            && SymbolEqualityComparer.Default.Equals(namedType.TypeArguments[1], contract.ValueType);
    }

    private static bool IsDataLoaderContract(
        INamedTypeSymbol type,
        INamedTypeSymbol batchDataLoader,
        INamedTypeSymbol cacheDataLoader)
        => SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, batchDataLoader)
            || SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, cacheDataLoader);

    private static bool IsClosed(ITypeSymbol type)
        => type switch
        {
            ITypeParameterSymbol => false,
            IArrayTypeSymbol arrayType => IsClosed(arrayType.ElementType),
            INamedTypeSymbol namedType => namedType.TypeArguments.All(IsClosed),
            _ => true
        };

    private static bool TryGetAsyncResultType(
        ITypeSymbol returnType,
        Compilation compilation,
        out ITypeSymbol resultType)
    {
        var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            && (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, task)
                || SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, valueTask)))
        {
            resultType = namedType.TypeArguments[0];
            return true;
        }

        resultType = null!;
        return false;
    }
}

internal readonly record struct DataLoaderAttributeInfo(
    AttributeSyntax Syntax,
    INamedTypeSymbol Type,
    bool IsGeneric);

internal readonly record struct DataLoaderContract(
    DataLoaderKind Kind,
    ITypeSymbol KeyType,
    ITypeSymbol ValueType);
