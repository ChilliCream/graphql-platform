using Microsoft.CodeAnalysis.CSharp;

namespace Mocha.Analyzers.Utils;

internal static class GeneratedMetadataWriter
{
    public static string ToSourceStringLiteral(this string value) => SymbolDisplay.FormatLiteral(value, quote: true);

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

        if (xmlDocumentation is null && location is null)
        {
            return;
        }

        var directory = DeriveDeclarationDirectory(location?.FilePath, options.SourceRoots);
        var suffix = trailingComma ? "," : string.Empty;

        writer.WriteIndentedLine("Source = new {0}", metadataTypeName);
        writer.WriteIndentedLine("{");
        writer.IncreaseIndent();
        WriteAssembly(writer, assemblyName);
        WriteRepositoryUrl(writer, options.RepositoryUrl);
        WriteCommit(writer, options.Commit);
        WriteXmlDocumentation(writer, xmlDocumentation, trailingComma: location is not null);

        if (location is not null)
        {
            WriteDeclarationLocation(
                writer,
                locationTypeName,
                GetFileName(location.FilePath),
                directory,
                location.StartLine,
                location.StartColumn,
                location.EndLine,
                location.EndColumn,
                trailingComma: false);
        }

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

        if (xmlDocumentation is null && location is null)
        {
            return;
        }

        var directory = DeriveDeclarationDirectory(location?.FilePath, options.SourceRoots);

        writer.WriteIndentedLine("{0} = new {1}", target, metadataTypeName);
        writer.WriteIndentedLine("{");
        writer.IncreaseIndent();
        WriteAssembly(writer, assemblyName);
        WriteRepositoryUrl(writer, options.RepositoryUrl);
        WriteCommit(writer, options.Commit);
        WriteXmlDocumentation(writer, xmlDocumentation, trailingComma: location is not null);

        if (location is not null)
        {
            WriteDeclarationLocation(
                writer,
                locationTypeName,
                GetFileName(location.FilePath),
                directory,
                location.StartLine,
                location.StartColumn,
                location.EndLine,
                location.EndColumn,
                trailingComma: false);
        }

        writer.DecreaseIndent();
        writer.WriteIndentedLine("};");
    }

    private static string GetFileName(string filePath)
    {
        if (filePath.Length == 0)
        {
            return filePath;
        }

        var normalized = filePath.Replace('\\', '/');
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

    private static void WriteXmlDocumentation(CodeWriter writer, string? xmlDocumentation, bool trailingComma = true)
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
        string file,
        string? directory,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        bool trailingComma = true)
    {
        var suffix = trailingComma ? "," : string.Empty;
        var directoryLiteral = directory is null ? "null" : directory.ToSourceStringLiteral();

        writer.WriteIndentedLine(
            "DeclarationLocation = new {0}({1}, {2}, {3}, {4}, {5}, {6}){7}",
            locationTypeName,
            file.ToSourceStringLiteral(),
            directoryLiteral,
            startLine,
            startColumn,
            endLine,
            endColumn,
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

    // A flattened SourceRoot record is "Identity>MappedPath>SourceControl"; records are joined
    // by '|'. These separators match the buildTransitive target that produces
    // build_property._MochaSourceRoots. File system paths legally contain ';' far more often
    // than '|' or '>', so these separators avoid collisions with real paths.
    private const char SourceRootRecordSeparator = '|';
    private const char SourceRootFieldSeparator = '>';

    // Derives the declaration file's directory path relative to the repository root from the
    // flattened SourceLink source roots. Returns null when no root information is available, no root
    // matches, or the matched suffix is empty.
    private static string? DeriveDeclarationDirectory(string? declarationFilePath, string? sourceRoots)
    {
        if (string.IsNullOrEmpty(declarationFilePath) || string.IsNullOrEmpty(sourceRoots))
        {
            return null;
        }

        var normalizedFilePath = declarationFilePath!.Replace('\\', '/');

        string? bestSuffix = null;
        var bestHasSourceControl = false;
        var bestPrefixLength = -1;

        foreach (var record in sourceRoots!.Split(SourceRootRecordSeparator))
        {
            var fields = record.Split(SourceRootFieldSeparator);

            // Skip malformed records that do not carry Identity, MappedPath, and SourceControl.
            if (fields.Length < 3)
            {
                continue;
            }

            var hasSourceControl = !string.IsNullOrEmpty(fields[2]);

            // A file matches either the root's local Identity path or its build-mapped path
            // (deterministic builds remap absolute paths, for example to "/_/").
            TryConsiderRoot(
                normalizedFilePath,
                fields[0],
                hasSourceControl,
                ref bestSuffix,
                ref bestHasSourceControl,
                ref bestPrefixLength);
            TryConsiderRoot(
                normalizedFilePath,
                fields[1],
                hasSourceControl,
                ref bestSuffix,
                ref bestHasSourceControl,
                ref bestPrefixLength);
        }

        return GetDirectoryPath(bestSuffix);
    }

    private static string? GetDirectoryPath(string? repositoryRelativeFilePath)
    {
        if (string.IsNullOrEmpty(repositoryRelativeFilePath))
        {
            return null;
        }

        var lastSeparator = repositoryRelativeFilePath!.LastIndexOf('/');

        return lastSeparator < 0 ? string.Empty : repositoryRelativeFilePath.Substring(0, lastSeparator);
    }

    private static void TryConsiderRoot(
        string normalizedFilePath,
        string root,
        bool hasSourceControl,
        ref string? bestSuffix,
        ref bool bestHasSourceControl,
        ref int bestPrefixLength)
    {
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var normalizedRoot = root.Replace('\\', '/');

        if (!normalizedRoot.EndsWith("/", StringComparison.Ordinal))
        {
            normalizedRoot += "/";
        }

        if (!normalizedFilePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var prefixLength = normalizedRoot.Length;

        // Prefer a source-control-backed root; among equal source-control status, the longest
        // matched prefix wins.
        var better =
            bestPrefixLength < 0
            || (hasSourceControl && !bestHasSourceControl)
            || (hasSourceControl == bestHasSourceControl && prefixLength > bestPrefixLength);

        if (!better)
        {
            return;
        }

        bestSuffix = normalizedFilePath.Substring(normalizedRoot.Length);
        bestHasSourceControl = hasSourceControl;
        bestPrefixLength = prefixLength;
    }
}
