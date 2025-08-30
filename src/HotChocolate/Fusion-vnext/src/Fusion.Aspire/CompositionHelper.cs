using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class CompositionHelper
{
    public static async Task<bool> TryComposeAsync(
        string fusionArchivePath,
        ImmutableArray<SourceSchemaInfo> sourceSchemas,
        string environmentName,
        GraphQLCompositionSettings settings,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
    {
        using var archive = File.Exists(fusionArchivePath)
            ? FusionArchive.Open(fusionArchivePath, FusionArchiveMode.Update)
            : FusionArchive.Create(fusionArchivePath);

        var sourceSchemaNames = new SortedSet<string>(
            await archive.GetSourceSchemaNamesAsync(cancellationToken),
            StringComparer.Ordinal);

        var sourceSchemaMap = sourceSchemas.ToDictionary(s => s.Name, StringComparer.Ordinal);

        foreach (var schemaName in sourceSchemaNames)
        {
            if (sourceSchemaMap.ContainsKey(schemaName))
            {
                continue;
            }

            var configuration = await archive.TryGetSourceSchemaConfigurationAsync(schemaName, cancellationToken);
            if (configuration is null)
            {
                continue;
            }

            var sourceText = await ReadSchemaSourceTextAsync(configuration, cancellationToken);

            sourceSchemaMap.Add(schemaName, new SourceSchemaInfo
            {
                Name = schemaName,
                Schema = new SourceSchemaText(schemaName, sourceText),
                SchemaSettings = configuration.Settings.RootElement.Clone()
            });
        }

        foreach (var schema in sourceSchemas)
        {
            if (!sourceSchemaNames.Add(schema.Name))
            {
                sourceSchemaMap[schema.Name] = new SourceSchemaInfo
                {
                    Name = schema.Name,
                    Schema = schema.Schema,
                    SchemaSettings = schema.SchemaSettings
                };
            }
        }

        var compositionLog = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            sourceSchemaMap.Values.OrderBy(t => t.Name).Select(t => t.Schema),
            new SchemaComposerOptions
            {
                EnableGlobalObjectIdentification = settings.EnableGlobalObjectIdentification
            },
            compositionLog);
        var result = schemaComposer.Compose();

        var output = new StringBuilder();

        foreach (var entry in compositionLog)
        {
            if (entry.Severity is LogSeverity.Error)
            {
                output.AppendLine($"‼️ {entry.Message}");
            }
            else
            {
                output.AppendLine(entry.Message);
            }
        }

        if (result.IsSuccess)
        {
            using var buffer = new PooledArrayWriter(4096);
            var settingsComposer = new SettingsComposer();
            settingsComposer.Compose(buffer, sourceSchemaMap.Values.Select(t => t.SchemaSettings).ToArray(), environmentName);

            var metadata = new ArchiveMetadata
            {
                SupportedGatewayFormats = [new Version(2, 0, 0)],
                SourceSchemas = [.. sourceSchemaNames]
            };

            await archive.SetArchiveMetadataAsync(metadata, cancellationToken);

            foreach (var sourceSchema in sourceSchemaMap.Values)
            {
                await archive.SetSourceSchemaConfigurationAsync(
                    sourceSchema.Name,
                    Encoding.UTF8.GetBytes(sourceSchema.Schema.SourceText),
                    JsonDocument.Parse(sourceSchema.SchemaSettings.GetRawText()),
                    cancellationToken);
            }

            await archive.SetGatewayConfigurationAsync(
                result.Value.ToString(),
                JsonDocument.Parse(buffer.WrittenMemory),
                new Version(2, 0, 0),
                cancellationToken);

            await archive.CommitAsync(cancellationToken);

            output.Append("✅ Composition completed successfully.");
            logger.LogInformation("{Message}", output.ToString());
            return true;
        }

        output.Append("❌ Composition failed:");
        logger.LogError("{Message}", output.ToString());
        return false;
    }

    private static async Task<string> ReadSchemaSourceTextAsync(
        SourceSchemaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await using var stream = await configuration.OpenReadSchemaAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
