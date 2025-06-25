using System.Collections.Immutable;
using System.Text.RegularExpressions;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

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
                return new ModuleInfo(SanitizeIdentifier(module.ModuleName), module.Options);
            }
        }

        if (syntaxInfos.Any(t => t is DataLoaderModuleInfo))
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

        return new DataLoaderDefaultsInfo(null, null, true, true, true);
    }

    public static DataLoaderDefaultsInfo GetDataLoaderDefaults(
        this List<SyntaxInfo> syntaxInfos)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is DataLoaderDefaultsInfo defaults)
            {
                return defaults;
            }
        }

        return new DataLoaderDefaultsInfo(null, null, true, true, true);
    }

    public static string CreateModuleName(string? assemblyName)
        => assemblyName is null
            ? "AssemblyTypes"
            : SanitizeIdentifier(assemblyName.Split('.').Last()) + "Types";

    public static string ConvertDefaultValueToString(object? defaultValue, ITypeSymbol type)
    {
        if (defaultValue == null)
        {
            return "default";
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return $"\"{defaultValue}\"";
        }

        if (type.SpecialType == SpecialType.System_Char)
        {
            return $"'{defaultValue}'";
        }

        if (type.SpecialType == SpecialType.System_Boolean)
        {
            return defaultValue.ToString()!.ToLower();
        }

        if (type.SpecialType == SpecialType.System_Double ||
            type.SpecialType == SpecialType.System_Single)
        {
            return $"{defaultValue}d";
        }

        if (type.SpecialType == SpecialType.System_Decimal)
        {
            return $"{defaultValue}m";
        }

        if (type.SpecialType == SpecialType.System_Int64 ||
            type.SpecialType == SpecialType.System_UInt64)
        {
            return $"{defaultValue}L";
        }

        return defaultValue.ToString()!;
    }

    public static string SanitizeIdentifier(string input)
    {
        Regex invalidCharsRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

        var sanitized = invalidCharsRegex.Replace(input, "_");

        if (!char.IsLetter(sanitized[0]))
        {
            sanitized = "_" + sanitized.Substring(1);
        }

        return sanitized;
    }
}
