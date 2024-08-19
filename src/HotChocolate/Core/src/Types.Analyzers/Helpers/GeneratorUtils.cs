using System.Collections.Immutable;
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

        if(syntaxInfos.Any(t => t is DataLoaderModuleInfo))
        {
            defaultModule = false;
            return new ModuleInfo(CreateModuleName(assemblyName), ModuleOptions.Disabled);
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

    public static string CreateModuleName(string? assemblyName)
        => assemblyName is null
            ? "AssemblyTypes"
            : assemblyName.Split('.').Last() + "Types";
}
