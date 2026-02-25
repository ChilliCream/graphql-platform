using System.Collections.Immutable;
using System.Text.RegularExpressions;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Helpers;

internal static class GeneratorUtils
{
    private static readonly Regex s_invalidCharsRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);
    private static readonly Regex s_xmlWhitespaceRegex = new(@"\n[ \t]*", RegexOptions.Compiled);

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

        if (type.SpecialType == SpecialType.System_Double
            || type.SpecialType == SpecialType.System_Single)
        {
            return $"{defaultValue}d";
        }

        if (type.SpecialType == SpecialType.System_Decimal)
        {
            return $"{defaultValue}m";
        }

        if (type.SpecialType == SpecialType.System_Int64
            || type.SpecialType == SpecialType.System_UInt64)
        {
            return $"{defaultValue}L";
        }

        if (type is INamedTypeSymbol namedTypeSymbol)
        {
            if (type.TypeKind == TypeKind.Enum)
            {
                var enumType = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Find the enum member that matches the default value
                foreach (var member in namedTypeSymbol.GetMembers())
                {
                    if (member is IFieldSymbol { HasConstantValue: true } field
                        && Equals(field.ConstantValue, defaultValue))
                    {
                        return $"{enumType}.{field.Name}";
                    }
                }

                // Fallback to integer value if no matching member found
                return defaultValue.ToString()!;
            }

            if (type.IsNullableValueType())
            {
                return ConvertDefaultValueToString(defaultValue, namedTypeSymbol.TypeArguments[0]);
            }
        }

        return defaultValue.ToString();
    }

    public static string SanitizeIdentifier(string input)
    {
        var sanitized = s_invalidCharsRegex.Replace(input, "_");

        if (!char.IsLetter(sanitized[0]))
        {
            sanitized = "_" + sanitized.Substring(1);
        }

        return sanitized;
    }

    /// <summary>
    /// Normalizes XML documentation text by removing common leading whitespace
    /// and standardizing line breaks. Handles multiline summaries properly.
    /// </summary>
    public static string? NormalizeXmlDocumentation(string? documentation)
    {
        if (string.IsNullOrWhiteSpace(documentation))
        {
            return null;
        }

        // Normalize line endings and trim outer newlines
        var normalized = documentation!.Replace("\r", string.Empty);
        if (normalized[0] == ' ')
        {
            normalized = "\n" + normalized;
        }

        // Find common leading whitespace pattern
        var whitespace = s_xmlWhitespaceRegex.Match(normalized).Value;

        // Remove common leading whitespace from all lines
        if (!string.IsNullOrEmpty(whitespace))
        {
            normalized = normalized.Replace(whitespace, "\n");
        }

        // Trim final result
        return normalized.Trim('\n').Trim();
    }

    /// <summary>
    /// Escapes a string for use in a C# string literal, handling special
    /// characters like quotes, backslashes, and line breaks.
    /// </summary>
    public static string? EscapeForStringLiteral(string? s)
    {
        if (s == null)
        {
            return null;
        }

        return s.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
