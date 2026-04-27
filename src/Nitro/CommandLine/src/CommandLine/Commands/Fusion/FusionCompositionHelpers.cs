using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal static class FusionCompositionHelpers
{
    public static bool IsSchemaFile(string? fileName)
    {
        if (fileName is null)
        {
            return false;
        }

        return fileName.EndsWith(".graphql", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".graphqls", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<Dictionary<string, (SourceSchemaText, JsonDocument)>> ReadSourceSchemasAsync(
        IFileSystem fileSystem,
        string? workingDirectory,
        IReadOnlyList<string> sourceSchemaFiles,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        foreach (var sourceSchemaFile in sourceSchemaFiles)
        {
            var (schemaName, sourceText, settings) = await ReadSourceSchemaAsync(
                fileSystem,
                workingDirectory,
                sourceSchemaFile,
                cancellationToken);

            sourceSchemas.Add(schemaName, (sourceText, settings));
        }

        return sourceSchemas;
    }

    public static async Task<(string SchemaName, SourceSchemaText SourceText, JsonDocument Settings)> ReadSourceSchemaAsync(
        IFileSystem fileSystem,
        string? workingDirectory,
        string sourceSchemaPath,
        CancellationToken cancellationToken)
    {
        if (!Path.IsPathRooted(sourceSchemaPath))
        {
            sourceSchemaPath = Path.Combine(
                workingDirectory ?? fileSystem.GetCurrentDirectory(),
                sourceSchemaPath);
        }

        string? schemaFilePath = null;

        if (fileSystem.DirectoryExists(sourceSchemaPath))
        {
            schemaFilePath =
                fileSystem
                    .GetFiles(sourceSchemaPath, "*.graphql*", SearchOption.AllDirectories)
                    .FirstOrDefault(f => IsSchemaFile(Path.GetFileName(f)));
        }
        else if (fileSystem.FileExists(sourceSchemaPath))
        {
            schemaFilePath = sourceSchemaPath;
        }

        if (schemaFilePath is null)
        {
            throw new ExitException(Messages.SchemaFileDoesNotExist(sourceSchemaPath));
        }

        var settingsFilePath = Path.Combine(
            Path.GetDirectoryName(schemaFilePath)!,
            Path.GetFileNameWithoutExtension(schemaFilePath) + "-settings.json");

        if (!fileSystem.FileExists(settingsFilePath))
        {
            throw new ExitException(Messages.SchemaSettingsFileDoesNotExist(settingsFilePath));
        }

        var settings = JsonDocument.Parse(
            await fileSystem.ReadAllBytesAsync(settingsFilePath, cancellationToken));
        var schemaName = settings.RootElement.GetProperty("name").GetString();

        if (schemaName is null)
        {
            throw new InvalidOperationException("Invalid source schema settings format.");
        }

        var sourceText = await fileSystem.ReadAllTextAsync(schemaFilePath, cancellationToken);

        return (schemaName, new SourceSchemaText(schemaName, sourceText), settings);
    }
}
