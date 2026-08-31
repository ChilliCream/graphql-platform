using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class DataLoaderGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, string> addSource)
    {
        var dataLoaderDefaults = syntaxInfos.GetDataLoaderDefaults();
        WriteDataLoader(syntaxInfos, dataLoaderDefaults, addSource);
    }

    private static void WriteDataLoader(
        ImmutableArray<SyntaxInfo> syntaxInfos,
        DataLoaderDefaultsInfo defaults,
        Action<string, string> addSource)
    {
        var dataLoaders = new List<DataLoaderInfo>();
        var genericDataLoaders = new List<GenericDataLoaderInfo>();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is DataLoaderInfo dataLoader)
            {
                if (dataLoader.Diagnostics.Length > 0)
                {
                    continue;
                }

                dataLoaders.Add(dataLoader);
            }
            else if (syntaxInfo is GenericDataLoaderInfo genericDataLoader)
            {
                genericDataLoaders.Add(genericDataLoader);
            }
        }

        var hasDataLoaders = false;

        using var generator = new DataLoaderFileBuilder();
        generator.WriteHeader();

        foreach (var @namespace in dataLoaders.Select(t => t.Namespace)
            .Concat(genericDataLoaders.Select(t => t.Namespace))
            .Distinct())
        {
            generator.WriteBeginNamespace(@namespace);

            foreach (var dataLoader in dataLoaders.Where(t => t.Namespace == @namespace))
            {
                var keyArg = dataLoader.MethodSymbol.Parameters[0];
                var keyType = keyArg.Type;

                if (IsKeysArgument(keyType))
                {
                    keyType = ExtractKeyType(keyType);
                }

                DataLoaderKind kind;

                if (IsReturnTypeDictionary(dataLoader.MethodSymbol.ReturnType, keyType))
                {
                    kind = DataLoaderKind.Batch;
                }
                else if (IsReturnTypeLookup(dataLoader.MethodSymbol.ReturnType, keyType))
                {
                    kind = DataLoaderKind.Group;
                }
                else
                {
                    keyType = keyArg.Type;
                    kind = DataLoaderKind.Cache;
                }

                var valueType = ExtractValueType(dataLoader.MethodSymbol.ReturnType, kind);

                if (hasDataLoaders)
                {
                    generator.WriteLine();
                }

                GenerateDataLoader(
                    generator,
                    dataLoader,
                    defaults,
                    kind,
                    keyType,
                    valueType);
                hasDataLoaders = true;
            }

            foreach (var dataLoader in genericDataLoaders.Where(t => t.Namespace == @namespace))
            {
                if (hasDataLoaders)
                {
                    generator.WriteLine();
                }

                GenerateDataLoader(generator, dataLoader, defaults);
                hasDataLoaders = true;
            }

            var groups = new Dictionary<string, List<GroupedDataLoaderInfo>>(StringComparer.Ordinal);
            var isInterfacePublic = defaults.IsInterfacePublic ?? true;

            foreach (var dataLoader in dataLoaders.Where(t => t.Namespace == @namespace))
            {
                foreach (var groupName in dataLoader.Groups)
                {
                    if (!groups.TryGetValue(groupName, out var group))
                    {
                        group = [];
                        groups.Add(groupName, group);
                    }

                    group.Add(
                        new GroupedDataLoaderInfo(
                            dataLoader.NameWithoutSuffix,
                            dataLoader.InterfaceName,
                            dataLoader.IsInterfacePublic ?? isInterfacePublic));
                }
            }

            foreach (var dataLoader in genericDataLoaders.Where(t => t.Namespace == @namespace))
            {
                foreach (var groupName in dataLoader.Groups)
                {
                    if (!groups.TryGetValue(groupName, out var group))
                    {
                        group = [];
                        groups.Add(groupName, group);
                    }

                    var dataLoaderType = genericDataLoaders.Count(
                        t => SymbolEqualityComparer.Default.Equals(t.DataLoaderType, dataLoader.DataLoaderType)) == 1
                        ? dataLoader.DataLoaderType.ToFullyQualifiedWithNullRefQualifier()
                        : dataLoader.Name;
                    group.Add(
                        new GroupedDataLoaderInfo(
                            dataLoader.NameWithoutSuffix,
                            dataLoaderType,
                            dataLoader.IsInterfacePublic ?? isInterfacePublic));
                }
            }

            foreach (var dataLoaderGroup in groups.OrderBy(t => t.Key, StringComparer.Ordinal))
            {
                generator.WriteDataLoaderGroupClass(
                    dataLoaderGroup.Key,
                    dataLoaderGroup.Value,
                    defaults.GenerateInterfaces,
                    isInterfacePublic,
                    defaults.IsPublic ?? true);
            }

            generator.WriteEndNamespace();
        }

        if (hasDataLoaders)
        {
            addSource(WellKnownFileNames.DataLoaderFile, generator.ToString());
        }
    }

    private static void GenerateDataLoader(
        DataLoaderFileBuilder generator,
        DataLoaderInfo dataLoader,
        DataLoaderDefaultsInfo defaults,
        DataLoaderKind kind,
        ITypeSymbol keyType,
        ITypeSymbol valueType)
    {
        var isScoped = dataLoader.IsScoped ?? defaults.Scoped ?? true;
        var isPublic = dataLoader.IsPublic ?? defaults.IsPublic ?? true;
        var isInterfacePublic = dataLoader.IsInterfacePublic ?? defaults.IsInterfacePublic ?? true;

        if (defaults.GenerateInterfaces)
        {
            generator.WriteDataLoaderInterface(
                dataLoader.InterfaceName,
                isInterfacePublic,
                kind,
                keyType,
                valueType);
        }

        generator.WriteBeginDataLoaderClass(
            dataLoader.Name,
            defaults.GenerateInterfaces ? dataLoader.InterfaceName : null,
            dataLoader.MethodSymbol,
            isPublic,
            kind,
            keyType,
            valueType);
        generator.WriteDataLoaderConstructor(
            dataLoader.Name,
            kind,
            keyType,
            valueType,
            dataLoader.GetLookups(keyType, valueType),
            dataLoader.MaxBatchSize);
        generator.WriteLine();
        generator.WriteDataLoaderLoadMethod(
            dataLoader.ContainingType,
            dataLoader.MethodSymbol,
            isScoped,
            kind,
            keyType,
            valueType,
            dataLoader.Parameters);
        generator.WriteEndDataLoaderClass();
    }

    private static void GenerateDataLoader(
        DataLoaderFileBuilder generator,
        GenericDataLoaderInfo dataLoader,
        DataLoaderDefaultsInfo defaults)
    {
        var isScoped = dataLoader.IsScoped ?? defaults.Scoped ?? true;
        var isPublic = dataLoader.IsPublic ?? defaults.IsPublic ?? true;

        generator.WriteBeginDataLoaderClass(
            dataLoader.Name,
            dataLoader.DataLoaderType.ToFullyQualifiedWithNullRefQualifier(),
            dataLoader.MethodSymbol,
            isPublic,
            dataLoader.Kind,
            dataLoader.KeyType,
            dataLoader.ValueType);
        generator.WriteDataLoaderConstructor(
            dataLoader.Name,
            dataLoader.Kind,
            dataLoader.KeyType,
            dataLoader.ValueType,
            dataLoader.GetLookups(),
            dataLoader.MaxBatchSize);
        generator.WriteLine();
        generator.WriteDataLoaderLoadMethod(
            dataLoader.ContainingType,
            dataLoader.MethodSymbol,
            isScoped,
            dataLoader.Kind,
            dataLoader.KeyType,
            dataLoader.ValueType,
            dataLoader.Parameters);
        generator.WriteEndDataLoaderClass();
    }

    private static bool IsKeysArgument(ITypeSymbol type)
        => type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } nt
            && WellKnownTypes.ReadOnlyList.Equals(ToTypeNameNoGenerics(nt), StringComparison.Ordinal);

    private static ITypeSymbol ExtractKeyType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType
            && WellKnownTypes.ReadOnlyList.Equals(ToTypeNameNoGenerics(namedType), StringComparison.Ordinal))
        {
            return namedType.TypeArguments[0];
        }

        throw new InvalidOperationException();
    }

    private static bool IsReturnTypeDictionary(ITypeSymbol returnType, ITypeSymbol keyType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            var resultType = namedType.TypeArguments[0];

            if (IsReadOnlyDictionaryInterface(resultType)
                && resultType is INamedTypeSymbol { TypeArguments.Length: 2 } dictionaryType1
                && dictionaryType1.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }

            if (IsDictionaryInterface(resultType)
                && resultType is INamedTypeSymbol { TypeArguments.Length: 2 } dictionaryType2
                && dictionaryType2.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReturnTypeLookup(ITypeSymbol returnType, ITypeSymbol keyType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            var resultType = namedType.TypeArguments[0];

            if (ToTypeNameNoGenerics(resultType).Equals(WellKnownTypes.Lookup, StringComparison.Ordinal)
                && resultType is INamedTypeSymbol { TypeArguments.Length: 2 } dictionaryType
                && dictionaryType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReadOnlyDictionaryInterface(ITypeSymbol type)
    {
        if (!ToTypeNameNoGenerics(type).Equals(WellKnownTypes.ReadOnlyDictionary, StringComparison.Ordinal))
        {
            foreach (var interfaceSymbol in type.Interfaces)
            {
                if (ToTypeNameNoGenerics(interfaceSymbol)
                    .Equals(WellKnownTypes.ReadOnlyDictionary, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private static bool IsDictionaryInterface(ITypeSymbol type)
    {
        if (!ToTypeNameNoGenerics(type).Equals(WellKnownTypes.DictionaryInterface, StringComparison.Ordinal))
        {
            foreach (var interfaceSymbol in type.Interfaces)
            {
                if (ToTypeNameNoGenerics(interfaceSymbol)
                    .Equals(WellKnownTypes.DictionaryInterface, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private static ITypeSymbol ExtractValueType(ITypeSymbol returnType, DataLoaderKind kind)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            if (kind is DataLoaderKind.Batch or DataLoaderKind.Group
                && namedType.TypeArguments[0] is INamedTypeSymbol { TypeArguments.Length: 2 } dict)
            {
                return dict.TypeArguments[1];
            }

            if (kind is DataLoaderKind.Cache)
            {
                return namedType.TypeArguments[0];
            }
        }

        throw new InvalidOperationException();
    }

    private static string ToTypeNameNoGenerics(ITypeSymbol typeSymbol)
        => $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}";
}
