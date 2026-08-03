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

    public static bool IsExtensionsFile(string? fileName)
    {
        if (fileName is null)
        {
            return false;
        }

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        return nameWithoutExt.EndsWith("-extensions", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<Dictionary<string, (SourceSchemaText, JsonDocument)>> ReadSourceSchemasAsync(
        IFileSystem fileSystem,
        string? workingDirectory,
        IReadOnlyList<string> sourceSchemaFiles,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        try
        {
            foreach (var sourceSchemaFile in sourceSchemaFiles)
            {
                var (schemaName, sourceText, settings) = await ReadSourceSchemaAsync(
                    fileSystem,
                    workingDirectory,
                    sourceSchemaFile,
                    cancellationToken);

                if (!sourceSchemas.TryAdd(schemaName, (sourceText, settings)))
                {
                    settings.Dispose();
                    throw new ExitException(
                        Messages.DuplicateSourceSchemaName(schemaName));
                }
            }

            return sourceSchemas;
        }
        catch
        {
            foreach (var (_, settings) in sourceSchemas.Values)
            {
                settings.Dispose();
            }

            throw;
        }
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
                    .FirstOrDefault(f =>
                    {
                        var name = Path.GetFileName(f);
                        return IsSchemaFile(name) && !IsExtensionsFile(name);
                    });
        }
        else if (fileSystem.FileExists(sourceSchemaPath))
        {
            if (IsExtensionsFile(Path.GetFileName(sourceSchemaPath)))
            {
                throw new ExitException(Messages.SchemaExtensionsFileCannotBeUsedAsSchemaFile(sourceSchemaPath));
            }

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

        if (!settings.RootElement.TryGetProperty("name", out var name)
            || name.ValueKind is not JsonValueKind.String
            || string.IsNullOrWhiteSpace(name.GetString()))
        {
            settings.Dispose();
            throw new ExitException(
                Messages.SourceSchemaSettingsNameInvalid(settingsFilePath));
        }

        var schemaName = name.GetString()!;

        try
        {
            var sourceText = await fileSystem.ReadAllTextAsync(
                schemaFilePath,
                cancellationToken);

            var extensionsFilePath = Path.Combine(
                Path.GetDirectoryName(schemaFilePath)!,
                Path.GetFileNameWithoutExtension(schemaFilePath)
                + "-extensions"
                + Path.GetExtension(schemaFilePath));

            string? extensionsSourceText = null;
            if (fileSystem.FileExists(extensionsFilePath))
            {
                extensionsSourceText = await fileSystem.ReadAllTextAsync(
                    extensionsFilePath,
                    cancellationToken);
            }

            return (
                schemaName,
                new SourceSchemaText(schemaName, sourceText, extensionsSourceText),
                settings);
        }
        catch
        {
            settings.Dispose();
            throw;
        }
    }

    public static async Task<Dictionary<string, (SourceSchemaText, JsonDocument)>>
        FetchRemoteSourceSchemasAsync(
            IFileSystem fileSystem,
            IReadOnlyList<RemoteSourceSchemaInput> inputs,
            IReadOnlySet<string> reservedSourceSchemaNames,
            HttpClient client,
            CancellationToken cancellationToken)
    {
        var preparedInputs = new List<PreparedRemoteSourceSchema>(inputs.Count);
        var sourceSchemaNames = new HashSet<string>(
            reservedSourceSchemaNames,
            StringComparer.Ordinal);

        try
        {
            foreach (var input in inputs)
            {
                if (!fileSystem.FileExists(input.SettingsFile))
                {
                    throw new ExitException(
                        Messages.SchemaSettingsFileDoesNotExist(input.SettingsFile));
                }

                var settings = JsonDocument.Parse(
                    await fileSystem.ReadAllBytesAsync(
                        input.SettingsFile,
                        cancellationToken));

                if (!settings.RootElement.TryGetProperty("name", out var name)
                    || name.ValueKind is not JsonValueKind.String
                    || string.IsNullOrWhiteSpace(name.GetString()))
                {
                    settings.Dispose();
                    throw new ExitException(
                        Messages.SourceSchemaSettingsNameInvalid(input.SettingsFile));
                }

                var sourceSchemaName = name.GetString()!;

                if (input.Name?.Equals(sourceSchemaName, StringComparison.Ordinal) is false)
                {
                    settings.Dispose();
                    throw new ExitException(Messages.WatchedSourceSchemaNameChanged());
                }

                if (!ApolloFederationSourceSchemaSettings.TryReadVersion(
                    sourceSchemaName,
                    settings.RootElement,
                    out var version,
                    out var errorMessage))
                {
                    settings.Dispose();
                    throw new ExitException(errorMessage);
                }

                if (!sourceSchemaNames.Add(sourceSchemaName))
                {
                    settings.Dispose();
                    throw new ExitException(
                        Messages.DuplicateSourceSchemaName(sourceSchemaName));
                }

                preparedInputs.Add(
                    new PreparedRemoteSourceSchema(
                        input,
                        sourceSchemaName,
                        settings,
                        version));
            }

            var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

            foreach (var input in preparedInputs)
            {
                var sourceText = input.Version is null
                    ? await FetchSchemaDocumentAsync(
                        client,
                        input.Name,
                        input.Input.Endpoint,
                        cancellationToken)
                    : await FetchApolloFederationSchemaAsync(
                        client,
                        input.Name,
                        input.Input.Endpoint,
                        cancellationToken);

                sourceSchemas.Add(
                    input.Name,
                    (new SourceSchemaText(input.Name, sourceText), input.Settings));
            }

            foreach (var input in preparedInputs)
            {
                input.Input.Name ??= input.Name;
            }

            return sourceSchemas;
        }
        catch
        {
            foreach (var input in preparedInputs)
            {
                input.Settings.Dispose();
            }

            throw;
        }
    }

    private static async Task<string> FetchSchemaDocumentAsync(
        HttpClient client,
        string sourceSchemaName,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            return await DefaultSchemaFetcher.FetchAsync(
                client,
                sourceSchemaName,
                endpoint,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ExitException(
                Messages.SourceSchemaTransportFailed(sourceSchemaName));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExitException(
                Messages.SourceSchemaTransportFailed(sourceSchemaName));
        }
        catch (IOException)
        {
            throw new ExitException(
                Messages.SourceSchemaTransportFailed(sourceSchemaName));
        }
    }

    private static async Task<string> FetchApolloFederationSchemaAsync(
        HttpClient client,
        string sourceSchemaName,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ApolloFederationSchemaFetcher.FetchAsync(
                client,
                sourceSchemaName,
                endpoint,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ExitException(
                Messages.SourceSchemaTransportFailed(sourceSchemaName));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExitException(
                Messages.SourceSchemaTransportFailed(sourceSchemaName));
        }
        catch (IOException)
        {
            throw new ExitException(
                Messages.SourceSchemaTransportFailed(sourceSchemaName));
        }
    }

    private readonly record struct PreparedRemoteSourceSchema(
        RemoteSourceSchemaInput Input,
        string Name,
        JsonDocument Settings,
        ApolloFederationVersion? Version);
}
