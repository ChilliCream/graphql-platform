using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class GenericDataLoaderInfo : SyntaxInfo
{
    private readonly string[] _lookups;

    private GenericDataLoaderInfo(
        AttributeSyntax attributeSyntax,
        IMethodSymbol attributeSymbol,
        AttributeData attributeData,
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax methodSyntax,
        ITypeSymbol dataLoaderType,
        DataLoaderKind kind,
        ITypeSymbol keyType,
        ITypeSymbol valueType)
    {
        AttributeSyntax = attributeSyntax;
        AttributeSymbol = attributeSymbol;
        AttributeData = attributeData;
        MethodSymbol = methodSymbol;
        MethodSyntax = methodSyntax;
        DataLoaderType = dataLoaderType;
        Kind = kind;
        KeyType = keyType;
        ValueType = valueType;
        _lookups = attributeData.GetLookups();

        NameWithoutSuffix = DataLoaderInfo.GetDataLoaderName(methodSymbol.Name, attributeData);
        Name = NameWithoutSuffix + "DataLoader";
        InterfaceName = $"I{Name}";
        Namespace = methodSymbol.ContainingNamespace.ToDisplayString();
        FullName = $"{Namespace}.{Name}";
        InterfaceFullName = $"{Namespace}.{InterfaceName}";
        IsScoped = attributeData.IsScoped();
        IsPublic = attributeData.IsPublic();
        IsInterfacePublic = attributeData.IsInterfacePublic();
        MaxBatchSize = attributeData.GetMaxBatchSize();
        KeyParameter = methodSymbol.Parameters[0];
        ContainingType = methodSymbol.ContainingType.ToDisplayString();
        Parameters = DataLoaderInfo.CreateParameters(methodSymbol);
        Groups = methodSymbol.GetDataLoaderGroupKeys();
    }

    public string Name { get; }

    public string NameWithoutSuffix { get; }

    public string FullName { get; }

    public ImmutableHashSet<string> Groups { get; }

    public string Namespace { get; }

    public string InterfaceName { get; }

    public string InterfaceFullName { get; }

    public string ContainingType { get; }

    public bool? IsScoped { get; }

    public bool? IsPublic { get; }

    public bool? IsInterfacePublic { get; }

    public int? MaxBatchSize { get; }

    public AttributeSyntax AttributeSyntax { get; }

    public IMethodSymbol AttributeSymbol { get; }

    public AttributeData AttributeData { get; }

    public IMethodSymbol MethodSymbol { get; }

    public MethodDeclarationSyntax MethodSyntax { get; }

    public ITypeSymbol DataLoaderType { get; }

    public DataLoaderKind Kind { get; }

    public ITypeSymbol KeyType { get; }

    public ITypeSymbol ValueType { get; }

    public IParameterSymbol KeyParameter { get; }

    public ImmutableArray<DataLoaderParameterInfo> Parameters { get; }

    public override string OrderByKey => FullName;

    public ImmutableArray<CacheLookup> GetLookups()
    {
        if (_lookups.Length == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<CacheLookup>();

        foreach (var lookup in _lookups)
        {
            foreach (var method in MethodSymbol.ContainingType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.Name == lookup && m.MethodKind is MethodKind.Ordinary))
            {
                if (method.Parameters.Length == 1
                    && method.Parameters[0].Type.Equals(ValueType, SymbolEqualityComparer.Default)
                    && method.ReturnType.Equals(KeyType, SymbolEqualityComparer.Default))
                {
                    builder.Add(new CacheLookup(method));
                }

                if (method.Parameters.Length == 1
                    && DataLoaderInfo.IsKeyValuePair(method.ReturnType, KeyType, ValueType))
                {
                    builder.Add(new CacheLookup(method, isTransform: true));
                }
            }
        }

        return builder.ToImmutable();
    }

    public static bool TryCreate(
        AttributeSyntax attributeSyntax,
        IMethodSymbol attributeSymbol,
        AttributeData attributeData,
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax methodSyntax,
        out GenericDataLoaderInfo? dataLoaderInfo)
    {
        if (!TryResolveContract(
                attributeSymbol.ContainingType.TypeArguments[0],
                out var kind,
                out var keyType,
                out var valueType)
            || !HasValidMethodShape(methodSymbol, kind, keyType, valueType))
        {
            dataLoaderInfo = null;
            return false;
        }

        dataLoaderInfo = new GenericDataLoaderInfo(
            attributeSyntax,
            attributeSymbol,
            attributeData,
            methodSymbol,
            methodSyntax,
            attributeSymbol.ContainingType.TypeArguments[0],
            kind,
            keyType,
            valueType);
        return true;
    }

    private static bool TryResolveContract(
        ITypeSymbol dataLoaderType,
        out DataLoaderKind kind,
        out ITypeSymbol keyType,
        out ITypeSymbol valueType)
    {
        if (dataLoaderType is not INamedTypeSymbol
            {
                TypeKind: TypeKind.Interface
            } namedDataLoaderType
            || !IsClosed(dataLoaderType))
        {
            kind = default;
            keyType = null!;
            valueType = null!;
            return false;
        }

        var contracts = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        if (IsDataLoaderKindInterface(namedDataLoaderType))
        {
            contracts.Add(namedDataLoaderType);
        }

        foreach (var interfaceType in namedDataLoaderType.AllInterfaces)
        {
            if (IsDataLoaderKindInterface(interfaceType))
            {
                contracts.Add(interfaceType);
            }
        }

        if (contracts.Count != 1)
        {
            kind = default;
            keyType = null!;
            valueType = null!;
            return false;
        }

        var contract = contracts[0];
        kind = GetTypeNameWithoutGenerics(contract) switch
        {
            WellKnownTypes.BatchDataLoader => DataLoaderKind.Batch,
            WellKnownTypes.CacheDataLoader => DataLoaderKind.Cache,
            _ => throw new InvalidOperationException()
        };
        keyType = contract.TypeArguments[0];
        valueType = contract.TypeArguments[1];
        return true;
    }

    private static bool HasValidMethodShape(
        IMethodSymbol methodSymbol,
        DataLoaderKind kind,
        ITypeSymbol keyType,
        ITypeSymbol valueType)
    {
        if (methodSymbol.Parameters.Length == 0
            || methodSymbol.IsGenericMethod
            || methodSymbol.DeclaredAccessibility is not (
                Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedAndInternal))
        {
            return false;
        }

        return kind is DataLoaderKind.Batch
            ? IsBatchMethod(methodSymbol, keyType, valueType)
            : IsCacheMethod(methodSymbol, keyType, valueType);
    }

    private static bool IsBatchMethod(
        IMethodSymbol methodSymbol,
        ITypeSymbol keyType,
        ITypeSymbol valueType)
        => IsReadOnlyList(methodSymbol.Parameters[0].Type, keyType)
            && TryGetAsyncResultType(methodSymbol.ReturnType, out var resultType)
            && IsDictionary(resultType, keyType, valueType);

    private static bool IsCacheMethod(
        IMethodSymbol methodSymbol,
        ITypeSymbol keyType,
        ITypeSymbol valueType)
        => methodSymbol.Parameters[0].Type.Equals(keyType, SymbolEqualityComparer.Default)
            && TryGetAsyncResultType(methodSymbol.ReturnType, out var resultType)
            && resultType.Equals(valueType, SymbolEqualityComparer.Default);

    private static bool IsDataLoaderKindInterface(INamedTypeSymbol type)
    {
        var name = GetTypeNameWithoutGenerics(type);
        return name.Equals(WellKnownTypes.BatchDataLoader, StringComparison.Ordinal)
            || name.Equals(WellKnownTypes.CacheDataLoader, StringComparison.Ordinal);
    }

    private static bool IsClosed(ITypeSymbol type)
        => type switch
        {
            ITypeParameterSymbol => false,
            IArrayTypeSymbol arrayType => IsClosed(arrayType.ElementType),
            INamedTypeSymbol namedType => namedType.TypeArguments.All(IsClosed),
            _ => true
        };

    private static bool IsReadOnlyList(ITypeSymbol type, ITypeSymbol keyType)
        => type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            && GetTypeNameWithoutGenerics(namedType).Equals(WellKnownTypes.ReadOnlyList, StringComparison.Ordinal)
            && namedType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default);

    private static bool IsDictionary(
        ITypeSymbol type,
        ITypeSymbol keyType,
        ITypeSymbol valueType)
        => type is INamedTypeSymbol { TypeArguments.Length: 2 } namedType
            && (GetTypeNameWithoutGenerics(namedType).Equals(WellKnownTypes.ReadOnlyDictionary, StringComparison.Ordinal)
                || GetTypeNameWithoutGenerics(namedType).Equals(WellKnownTypes.DictionaryInterface, StringComparison.Ordinal))
            && namedType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default)
            && namedType.TypeArguments[1].Equals(valueType, SymbolEqualityComparer.Default);

    private static bool TryGetAsyncResultType(
        ITypeSymbol returnType,
        out ITypeSymbol resultType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            && (GetTypeNameWithoutGenerics(namedType).Equals(WellKnownTypes.Task, StringComparison.Ordinal)
                || GetTypeNameWithoutGenerics(namedType).Equals(WellKnownTypes.ValueTask, StringComparison.Ordinal)))
        {
            resultType = namedType.TypeArguments[0];
            return true;
        }

        resultType = null!;
        return false;
    }

    private static string GetTypeNameWithoutGenerics(ITypeSymbol type)
        => $"{type.ContainingNamespace}.{type.Name}";

    public override bool Equals(object? obj)
        => obj is GenericDataLoaderInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is GenericDataLoaderInfo other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, AttributeSyntax, MethodSyntax);

    private bool Equals(GenericDataLoaderInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return OrderByKey.Equals(other.OrderByKey, StringComparison.Ordinal)
            && AttributeSyntax.IsEquivalentTo(other.AttributeSyntax)
            && MethodSyntax.IsEquivalentTo(other.MethodSyntax);
    }
}
