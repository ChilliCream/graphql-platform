using Microsoft.CodeAnalysis.CSharp;

namespace Mocha.Analyzers.Utils;

internal static class GeneratedMetadataWriter
{
    public static string ToSourceStringLiteral(this string value)
        => SymbolDisplay.FormatLiteral(value, quote: true);

    public static void WriteSourceMetadata(
        this CodeWriter writer,
        string metadataTypeName,
        string locationTypeName,
        string assemblyName,
        string? xmlDocumentation,
        LocationInfo? location,
        SourceMetadataOptionsInfo options,
        bool trailingComma = true)
    {
        if (!options.Emit)
        {
            return;
        }

        location = NormalizeLocation(location, options.ProjectDir);

        if (xmlDocumentation is null && location is null)
        {
            return;
        }

        var suffix = trailingComma ? "," : string.Empty;

        writer.WriteIndentedLine("Source = new {0}", metadataTypeName);
        writer.WriteIndentedLine("{");
        writer.IncreaseIndent();
        WriteAssembly(writer, assemblyName);
        WriteRepositoryUrl(writer, options.RepositoryUrl);
        WriteCommit(writer, options.Commit);
        WriteXmlDocumentation(writer, xmlDocumentation, trailingComma: location is not null);
        WriteDeclarationLocation(writer, locationTypeName, location, trailingComma: false);
        writer.DecreaseIndent();
        writer.WriteIndentedLine("}}{0}", suffix);
    }

    public static void WriteSourceMetadataAssignment(
        this CodeWriter writer,
        string target,
        string metadataTypeName,
        string locationTypeName,
        string assemblyName,
        string? xmlDocumentation,
        LocationInfo? location,
        SourceMetadataOptionsInfo options)
    {
        if (!options.Emit)
        {
            return;
        }

        location = NormalizeLocation(location, options.ProjectDir);

        if (xmlDocumentation is null && location is null)
        {
            return;
        }

        writer.WriteIndentedLine("{0} = new {1}", target, metadataTypeName);
        writer.WriteIndentedLine("{");
        writer.IncreaseIndent();
        WriteAssembly(writer, assemblyName);
        WriteRepositoryUrl(writer, options.RepositoryUrl);
        WriteCommit(writer, options.Commit);
        WriteXmlDocumentation(writer, xmlDocumentation, trailingComma: location is not null);
        WriteDeclarationLocation(writer, locationTypeName, location, trailingComma: false);
        writer.DecreaseIndent();
        writer.WriteIndentedLine("};");
    }

    private static LocationInfo? NormalizeLocation(LocationInfo? location, string? projectDir)
    {
        if (location is null)
        {
            return null;
        }

        return location with { FilePath = NormalizeFilePath(location.FilePath, projectDir) };
    }

    private static string NormalizeFilePath(string filePath, string? projectDir)
    {
        if (filePath.Length == 0)
        {
            return filePath;
        }

        var normalized = filePath.Replace('\\', '/');

        if (!string.IsNullOrEmpty(projectDir))
        {
            var normalizedProjectDir = projectDir!.Replace('\\', '/');

            if (!normalizedProjectDir.EndsWith("/", StringComparison.Ordinal))
            {
                normalizedProjectDir += "/";
            }

            if (normalized.StartsWith(normalizedProjectDir, StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(normalizedProjectDir.Length);
            }
        }

        var lastSeparator = normalized.LastIndexOf('/');

        return lastSeparator < 0 ? normalized : normalized.Substring(lastSeparator + 1);
    }

    private static void WriteAssembly(CodeWriter writer, string assemblyName)
        => writer.WriteIndentedLine("Assembly = {0},", assemblyName.ToSourceStringLiteral());

    private static void WriteRepositoryUrl(CodeWriter writer, string? repositoryUrl)
    {
        if (repositoryUrl is null)
        {
            return;
        }

        writer.WriteIndentedLine("RepositoryUrl = {0},", repositoryUrl.ToSourceStringLiteral());
    }

    private static void WriteCommit(CodeWriter writer, string? commit)
    {
        if (commit is null)
        {
            return;
        }

        writer.WriteIndentedLine("Commit = {0},", commit.ToSourceStringLiteral());
    }

    private static void WriteXmlDocumentation(
        CodeWriter writer,
        string? xmlDocumentation,
        bool trailingComma = true)
    {
        if (xmlDocumentation is null)
        {
            return;
        }

        var delimiter = new string('"', GetRawStringDelimiterLength(xmlDocumentation));
        var indent = writer.GetIndentString();
        var suffix = trailingComma ? "," : string.Empty;

        writer.WriteIndentedLine("XmlDocumentation = {0}", delimiter);

        foreach (var line in SplitLines(xmlDocumentation))
        {
            writer.Write(indent);
            writer.Write(line);
            writer.WriteLine();
        }

        writer.WriteIndentedLine("{0}{1}", delimiter, suffix);
    }

    private static void WriteDeclarationLocation(
        CodeWriter writer,
        string locationTypeName,
        LocationInfo? location,
        bool trailingComma = true)
    {
        if (location is null)
        {
            return;
        }

        var suffix = trailingComma ? "," : string.Empty;

        writer.WriteIndentedLine(
            "DeclarationLocation = new {0}({1}, {2}, {3}, {4}, {5}){6}",
            locationTypeName,
            location.FilePath.ToSourceStringLiteral(),
            location.StartLine,
            location.StartColumn,
            location.EndLine,
            location.EndColumn,
            suffix);
    }

    private static int GetRawStringDelimiterLength(string value)
    {
        var maxQuotes = 0;
        var currentQuotes = 0;

        foreach (var character in value)
        {
            if (character == '"')
            {
                currentQuotes++;
                maxQuotes = Math.Max(maxQuotes, currentQuotes);
            }
            else
            {
                currentQuotes = 0;
            }
        }

        return Math.Max(3, maxQuotes + 1);
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        using var reader = new StringReader(value);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            yield return line;
        }
    }
}
