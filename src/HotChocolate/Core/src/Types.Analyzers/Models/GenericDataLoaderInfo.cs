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
        Compilation compilation,
        out GenericDataLoaderInfo? dataLoaderInfo)
    {
        var wellKnownTypes = DataLoaderSymbols.TryCreate(compilation);

        if (wellKnownTypes is null
            || !SymbolEqualityComparer.Default.Equals(
                attributeSymbol.ContainingType.OriginalDefinition,
                wellKnownTypes.GenericDataLoaderAttribute)
            || !TryResolveContract(
                attributeSymbol.ContainingType.TypeArguments[0],
                wellKnownTypes,
                out var kind,
                out var keyType,
                out var valueType)
            || !HasValidMethodShape(methodSymbol, kind, keyType, valueType, wellKnownTypes))
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
        DataLoaderSymbols wellKnownTypes,
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

        if (IsDataLoaderKindInterface(namedDataLoaderType, wellKnownTypes))
        {
            contracts.Add(namedDataLoaderType);
        }

        foreach (var interfaceType in namedDataLoaderType.AllInterfaces)
        {
            if (IsDataLoaderKindInterface(interfaceType, wellKnownTypes))
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
        kind = SymbolEqualityComparer.Default.Equals(
            contract.ConstructedFrom,
            wellKnownTypes.BatchDataLoader)
            ? DataLoaderKind.Batch
            : DataLoaderKind.Cache;
        keyType = contract.TypeArguments[0];
        valueType = contract.TypeArguments[1];
        return true;
    }

    private static bool HasValidMethodShape(
        IMethodSymbol methodSymbol,
        DataLoaderKind kind,
        ITypeSymbol keyType,
        ITypeSymbol valueType,
        DataLoaderSymbols wellKnownTypes)
    {
        if (methodSymbol.Parameters.Length == 0
            || methodSymbol.IsGenericMethod
            || methodSymbol.ReturnsByRef
            || methodSymbol.ReturnsByRefReadonly
            || methodSymbol.Parameters.Any(t => t.RefKind is not RefKind.None)
            || methodSymbol.DeclaredAccessibility is not (
                Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedAndInternal))
        {
            return false;
        }

        return kind is DataLoaderKind.Batch
            ? IsBatchMethod(methodSymbol, keyType, valueType, wellKnownTypes)
            : IsCacheMethod(methodSymbol, keyType, valueType, wellKnownTypes);
    }

    private static bool IsBatchMethod(
        IMethodSymbol methodSymbol,
        ITypeSymbol keyType,
        ITypeSymbol valueType,
        DataLoaderSymbols wellKnownTypes)
        => IsReadOnlyList(methodSymbol.Parameters[0].Type, keyType, wellKnownTypes)
            && TryGetAsyncResultType(methodSymbol.ReturnType, wellKnownTypes, out var resultType)
            && IsDictionary(resultType, keyType, valueType, wellKnownTypes);

    private static bool IsCacheMethod(
        IMethodSymbol methodSymbol,
        ITypeSymbol keyType,
        ITypeSymbol valueType,
        DataLoaderSymbols wellKnownTypes)
        => methodSymbol.Parameters[0].Type.Equals(keyType, SymbolEqualityComparer.Default)
            && TryGetAsyncResultType(methodSymbol.ReturnType, wellKnownTypes, out var resultType)
            && resultType.Equals(valueType, SymbolEqualityComparer.Default);

    private static bool IsDataLoaderKindInterface(
        INamedTypeSymbol type,
        DataLoaderSymbols wellKnownTypes)
        => SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, wellKnownTypes.BatchDataLoader)
            || SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, wellKnownTypes.CacheDataLoader);

    private static bool IsClosed(ITypeSymbol type)
        => type switch
        {
            ITypeParameterSymbol => false,
            IArrayTypeSymbol arrayType => IsClosed(arrayType.ElementType),
            INamedTypeSymbol namedType => namedType.TypeArguments.All(IsClosed),
            _ => true
        };

    private static bool IsReadOnlyList(
        ITypeSymbol type,
        ITypeSymbol keyType,
        DataLoaderSymbols wellKnownTypes)
        => type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            && SymbolEqualityComparer.Default.Equals(
                namedType.ConstructedFrom,
                wellKnownTypes.ReadOnlyList)
            && namedType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default);

    private static bool IsDictionary(
        ITypeSymbol type,
        ITypeSymbol keyType,
        ITypeSymbol valueType,
        DataLoaderSymbols wellKnownTypes)
        => type is INamedTypeSymbol { TypeArguments.Length: 2 } namedType
            && (SymbolEqualityComparer.Default.Equals(
                    namedType.ConstructedFrom,
                    wellKnownTypes.ReadOnlyDictionary)
                || SymbolEqualityComparer.Default.Equals(
                    namedType.ConstructedFrom,
                    wellKnownTypes.DictionaryInterface))
            && namedType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default)
            && namedType.TypeArguments[1].Equals(valueType, SymbolEqualityComparer.Default);

    private static bool TryGetAsyncResultType(
        ITypeSymbol returnType,
        DataLoaderSymbols wellKnownTypes,
        out ITypeSymbol resultType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            && (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, wellKnownTypes.Task)
                || SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, wellKnownTypes.ValueTask)))
        {
            resultType = namedType.TypeArguments[0];
            return true;
        }

        resultType = null!;
        return false;
    }

    private sealed class DataLoaderSymbols(
        INamedTypeSymbol genericDataLoaderAttribute,
        INamedTypeSymbol batchDataLoader,
        INamedTypeSymbol cacheDataLoader,
        INamedTypeSymbol readOnlyList,
        INamedTypeSymbol readOnlyDictionary,
        INamedTypeSymbol dictionaryInterface,
        INamedTypeSymbol task,
        INamedTypeSymbol valueTask)
    {
        public INamedTypeSymbol GenericDataLoaderAttribute { get; } = genericDataLoaderAttribute;

        public INamedTypeSymbol BatchDataLoader { get; } = batchDataLoader;

        public INamedTypeSymbol CacheDataLoader { get; } = cacheDataLoader;

        public INamedTypeSymbol ReadOnlyList { get; } = readOnlyList;

        public INamedTypeSymbol ReadOnlyDictionary { get; } = readOnlyDictionary;

        public INamedTypeSymbol DictionaryInterface { get; } = dictionaryInterface;

        public INamedTypeSymbol Task { get; } = task;

        public INamedTypeSymbol ValueTask { get; } = valueTask;

        public static DataLoaderSymbols? TryCreate(Compilation compilation)
        {
            var genericDataLoaderAttribute = compilation.GetTypeByMetadataName(
                WellKnownAttributes.GenericDataLoaderAttribute);
            var batchDataLoader = compilation.GetTypeByMetadataName("GreenDonut.IBatchDataLoader`2");
            var cacheDataLoader = compilation.GetTypeByMetadataName("GreenDonut.ICacheDataLoader`2");
            var readOnlyList = compilation.GetTypeByMetadataName(
                "System.Collections.Generic.IReadOnlyList`1");
            var readOnlyDictionary = compilation.GetTypeByMetadataName(
                "System.Collections.Generic.IReadOnlyDictionary`2");
            var dictionaryInterface = compilation.GetTypeByMetadataName(
                "System.Collections.Generic.IDictionary`2");
            var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            return genericDataLoaderAttribute is not null
                && batchDataLoader is not null
                && cacheDataLoader is not null
                && readOnlyList is not null
                && readOnlyDictionary is not null
                && dictionaryInterface is not null
                && task is not null
                && valueTask is not null
                ? new DataLoaderSymbols(
                    genericDataLoaderAttribute,
                    batchDataLoader,
                    cacheDataLoader,
                    readOnlyList,
                    readOnlyDictionary,
                    dictionaryInterface,
                    task,
                    valueTask)
                : null;
        }
    }

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
