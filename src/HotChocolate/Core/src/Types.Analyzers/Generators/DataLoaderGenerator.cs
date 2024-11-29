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
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        var dataLoaderDefaults = syntaxInfos.GetDataLoaderDefaults();
        WriteDataLoader(context, syntaxInfos, dataLoaderDefaults);
    }

    private static void WriteDataLoader(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        DataLoaderDefaultsInfo defaults)
    {
        var dataLoaders = new List<DataLoaderInfo>();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is not DataLoaderInfo dataLoader)
            {
                continue;
            }

            if (dataLoader.Diagnostics.Length > 0)
            {
                continue;
            }

            dataLoaders.Add(dataLoader);
        }

        var hasDataLoaders = false;

        using var generator = new DataLoaderFileBuilder();
        generator.WriteHeader();

        foreach (var group in dataLoaders.GroupBy(t => t.Namespace))
        {
            generator.WriteBeginNamespace(group.Key);

            foreach (var dataLoader in group)
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

            List<GroupedDataLoaderInfo>? buffer = null;
            foreach (var dataLoaderGroup in group
                .Where(d => d.Groups.Count > 0)
                .SelectMany(d => d.Groups, (d, g) => new { DataLoader = d, Group = g })
                .GroupBy(t => t.Group, t => t.DataLoader, StringComparer.Ordinal)
                .OrderBy(t => t.Key, StringComparer.Ordinal))
            {
                var isPublic = defaults.IsInterfacePublic ?? true;
                var dataLoaderGroups = dataLoaderGroup.Select(
                    t => new GroupedDataLoaderInfo(
                        t.NameWithoutSuffix,
                        t.InterfaceName,
                        t.IsInterfacePublic ?? isPublic));

                buffer ??= new();
                buffer.Clear();
                buffer.AddRange(dataLoaderGroups);
                generator.WriteDataLoaderGroupClass(dataLoaderGroup.Key, buffer, defaults.GenerateInterfaces);
            }

            generator.WriteEndNamespace();
        }

        if (hasDataLoaders)
        {
            context.AddSource(WellKnownFileNames.DataLoaderFile, generator.ToSourceText());
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
            generator.WriteDataLoaderInterface(dataLoader.InterfaceName, isInterfacePublic, kind, keyType, valueType);
        }

        generator.WriteBeginDataLoaderClass(
            dataLoader.Name,
            dataLoader.InterfaceName,
            isPublic,
            kind,
            keyType,
            valueType,
            defaults.GenerateInterfaces);
        generator.WriteDataLoaderConstructor(
            dataLoader.Name,
            kind,
            keyType,
            valueType,
            dataLoader.GetLookups(keyType, valueType));
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

    private static bool IsKeysArgument(ITypeSymbol type)
        => type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1, } nt
            && WellKnownTypes.ReadOnlyList.Equals(ToTypeNameNoGenerics(nt), StringComparison.Ordinal);

    private static ITypeSymbol ExtractKeyType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1, } namedType
            && WellKnownTypes.ReadOnlyList.Equals(ToTypeNameNoGenerics(namedType), StringComparison.Ordinal))
        {
            return namedType.TypeArguments[0];
        }

        throw new InvalidOperationException();
    }

    private static bool IsReturnTypeDictionary(ITypeSymbol returnType, ITypeSymbol keyType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1, } namedType)
        {
            var resultType = namedType.TypeArguments[0];

            if (IsReadOnlyDictionaryInterface(resultType)
                && resultType is INamedTypeSymbol { TypeArguments.Length: 2, } dictionaryType1
                && dictionaryType1.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }

            if (IsDictionaryInterface(resultType)
                && resultType is INamedTypeSymbol { TypeArguments.Length: 2, } dictionaryType2
                && dictionaryType2.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReturnTypeLookup(ITypeSymbol returnType, ITypeSymbol keyType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1, } namedType)
        {
            var resultType = namedType.TypeArguments[0];

            if (ToTypeNameNoGenerics(resultType).Equals(WellKnownTypes.Lookup, StringComparison.Ordinal)
                && resultType is INamedTypeSymbol { TypeArguments.Length: 2, } dictionaryType
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
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1, } namedType)
        {
            if (kind is DataLoaderKind.Batch or DataLoaderKind.Group
                && namedType.TypeArguments[0] is INamedTypeSymbol { TypeArguments.Length: 2, } dict)
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
