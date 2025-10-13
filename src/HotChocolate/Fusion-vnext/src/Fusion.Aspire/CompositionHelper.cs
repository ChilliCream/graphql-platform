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
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
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

        var normalizedToRealExistingSchemaNameLookup =
            sourceSchemaNames.ToDictionary(StringUtilities.ToConstantCase, s => s);

        // During the schema merging process, schema names are converted to upper-case,
        // before being inserted into the fusion__Schema enum.
        // This means two different schema names, like some-service and SomeService,
        // could be uppercased to a conflicting SOME_SERVICE.
        // To avoid weird errors for the user down the line,
        // we already validate for collisions here.
        foreach (var newSourceSchema in newSourceSchemas)
        {
            var normalizedSchemaName = StringUtilities.ToConstantCase(newSourceSchema.Name);

            if (normalizedToRealExistingSchemaNameLookup.TryGetValue(normalizedSchemaName, out var existingSchemaName)
                && existingSchemaName != newSourceSchema.Name)
            {
                logger.LogError(
                    $"❌ '{newSourceSchema.Name}' conflicts with the existing source schema name '{existingSchemaName}'. "
                    + $"Either rename '{newSourceSchema.Name}' to '{existingSchemaName}' if they're the same, or "
                    + $"rename '{newSourceSchema.Name}' to something else if they're different.");
                return false;
            }
        }

        var sourceSchemas = newSourceSchemas.ToDictionary(s => s.Name, StringComparer.Ordinal);

        foreach (var schemaName in sourceSchemaNames)
        {
            if (sourceSchemas.ContainsKey(schemaName))
            {
                continue;
            }

            var configuration = await archive.TryGetSourceSchemaConfigurationAsync(schemaName, cancellationToken);
            if (configuration is null)
            {
                continue;
            }

            var sourceText = await ReadSchemaSourceTextAsync(configuration, cancellationToken);

            sourceSchemas.Add(schemaName, new SourceSchemaInfo
            {
                Name = schemaName,
                Schema = new SourceSchemaText(schemaName, sourceText),
                SchemaSettings = configuration.Settings.RootElement.Clone()
            });
        }

        foreach (var schema in newSourceSchemas)
        {
            if (!sourceSchemaNames.Add(schema.Name))
            {
                sourceSchemas[schema.Name] = new SourceSchemaInfo
                {
                    Name = schema.Name,
                    Schema = schema.Schema,
                    SchemaSettings = schema.SchemaSettings
                };
            }
        }

        var compositionLog = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            sourceSchemas.Values.Select(t => t.Schema),
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
                output.AppendLine($"‼️ {FormatMultilineMessage(entry.Message)}");
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
            settingsComposer.Compose(
                buffer,
                sourceSchemas.Values.Select(t => t.SchemaSettings).ToArray(),
                settings.EnvironmentName ?? "Aspire");

            var metadata = new ArchiveMetadata
            {
                SupportedGatewayFormats = [new Version(2, 0, 0)],
                SourceSchemas = [.. sourceSchemaNames]
            };

            await archive.SetArchiveMetadataAsync(metadata, cancellationToken);

            foreach (var sourceSchema in sourceSchemas.Values)
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

    /// <summary>
    /// Since we're prefixing the message with an emoji and space before printing,
    /// we need to also indent each line of a multiline message by three spaces to fix the alignment.
    /// </summary>
    private static string FormatMultilineMessage(string message)
    {
        var lines = message.Split(Environment.NewLine);

        if (lines.Length <= 1)
        {
            return message;
        }

        return string.Join(Environment.NewLine + "   ", lines);
    }
}
