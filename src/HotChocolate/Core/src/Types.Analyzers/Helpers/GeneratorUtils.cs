using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Inspectors;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.Helpers;

internal static class GeneratorUtils
{
    public static ModuleInfo GetModuleInfo(
        this ImmutableArray<SyntaxInfo> syntaxInfos,
        string? assemblyName,
        out bool defaultModule)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is ModuleInfo module)
            {
                defaultModule = false;
                return module;
            }
        }

        defaultModule = true;
        return new ModuleInfo(CreateModuleName(assemblyName), ModuleOptions.Default);
    }

    public static DataLoaderDefaultsInfo GetDataLoaderDefaults(
        this ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is DataLoaderDefaultsInfo defaults)
            {
                return defaults;
            }
        }

        return new DataLoaderDefaultsInfo(null, null, true, true);
    }

    private static string CreateModuleName(string? assemblyName)
        => assemblyName is null
            ? "AssemblyTypes"
            : assemblyName.Split('.').Last() + "Types";
}