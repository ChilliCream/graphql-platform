using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal static class CompositionHelper
{
    public static async Task<CompositionResult<MutableSchemaDefinition>> ComposeAsync(
        ICompositionLog compositionLog,
        Dictionary<string, (SourceSchemaText, JsonDocument)> sourceSchemas,
        FusionArchive archive,
        string environment,
        CompositionSettings? compositionSettings,
        CancellationToken cancellationToken)
    {
        var existingSourceSchemaNames = new SortedSet<string>(
            await archive.GetSourceSchemaNamesAsync(cancellationToken),
            StringComparer.Ordinal);

        var normalizedToRealExistingSchemaNameLookup =
            existingSourceSchemaNames.ToDictionary(StringUtilities.ToConstantCase, s => s);

        // During the schema merging process, schema names are converted to upper-case,
        // before being inserted into the fusion__Schema enum.
        // This means two different schema names, like some-service and SomeService,
        // could be uppercased to a conflicting SOME_SERVICE.
        // To avoid weird errors for the user down the line,
        // we already validate for collisions here.
        foreach (var (newSourceSchemaName, _) in sourceSchemas)
        {
            var normalizedSchemaName = StringUtilities.ToConstantCase(newSourceSchemaName);

            if (normalizedToRealExistingSchemaNameLookup.TryGetValue(normalizedSchemaName, out var existingSchemaName)
                && existingSchemaName != newSourceSchemaName)
            {
                compositionLog.Write(
                    LogEntryBuilder.New()
                        .SetMessage(
                            "'{0}' conflicts with the existing source schema name '{1}'. Either rename '{0}' to '{1}' if they're the same, or rename '{0}' to something else if they're different.",
                            newSourceSchemaName,
                            existingSchemaName)
                        .SetCode(LogEntryCodes.ConflictingSourceSchemaName)
                        .SetSeverity(LogSeverity.Error)
                        .Build());

                ImmutableArray<CompositionError> errors = [new("❌ Composition failed")];
                return errors;
            }
        }

        foreach (var schemaName in existingSourceSchemaNames)
        {
            if (sourceSchemas.ContainsKey(schemaName))
            {
                // We have a new configuration for the schema, so we'll take that
                // instead of the one in the gateway package.
                continue;
            }

            var configuration = await archive.TryGetSourceSchemaConfigurationAsync(schemaName, cancellationToken);

            if (configuration is null)
            {
                continue;
            }

            var sourceText = await ReadSchemaSourceTextAsync(configuration, cancellationToken);

            sourceSchemas[schemaName] = (new SourceSchemaText(schemaName, sourceText), configuration.Settings);
        }

        var existingCompositionSettings = await GetCompositionSettingsAsync(archive, cancellationToken);
        var mergedCompositionSettings =
            compositionSettings?.MergeInto(existingCompositionSettings) ?? existingCompositionSettings;

        var sourceSchemaOptionsMap = new Dictionary<string, SourceSchemaOptions>();
        var mergerOptions = mergedCompositionSettings.Merger.ToOptions();
        var satisfiabilityOptions = mergedCompositionSettings.Satisfiability.ToOptions();

        foreach (var (sourceSchemaName, (_, sourceSchemaSettings)) in sourceSchemas)
        {
            var schemaSettings =
                sourceSchemaSettings.Deserialize(SettingsJsonSerializerContext.Default.SourceSchemaSettings)!;

            var sourceSchemaOptions = schemaSettings.ToOptions();

            mergedCompositionSettings.Preprocessor?.MergeInto(sourceSchemaOptions.Preprocessor);
            sourceSchemaOptionsMap.Add(sourceSchemaName, sourceSchemaOptions);
            schemaSettings.Satisfiability?.MergeInto(satisfiabilityOptions);
        }

        var schemaComposerOptions = new SchemaComposerOptions
        {
            SourceSchemas = sourceSchemaOptionsMap,
            Merger = mergerOptions,
            Satisfiability = satisfiabilityOptions
        };

        var schemaComposer = new SchemaComposer(
            sourceSchemas.Select(s => s.Value.Item1),
            schemaComposerOptions,
            compositionLog);

        var result = schemaComposer.Compose();

        if (result.IsFailure)
        {
            return result;
        }

        using var bufferWriter = new PooledArrayWriter();
        new SettingsComposer().Compose(
            bufferWriter,
            sourceSchemas.Select(s => s.Value.Item2.RootElement).ToArray(),
            environment);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
            SourceSchemas = [.. sourceSchemas.Keys]
        };

        await archive.SetArchiveMetadataAsync(metadata, cancellationToken);

        foreach (var (schemaName, (schema, settings)) in sourceSchemas)
        {
            await archive.SetSourceSchemaConfigurationAsync(
                schemaName,
                Encoding.UTF8.GetBytes(schema.SourceText),
                settings,
                cancellationToken);
        }

        await archive.SetGatewayConfigurationAsync(
            result.Value + Environment.NewLine,
            JsonDocument.Parse(bufferWriter.WrittenMemory),
            WellKnownVersions.LatestGatewayFormatVersion,
            cancellationToken);

        await SaveCompositionSettingsAsync(archive, schemaComposerOptions, cancellationToken);

        await archive.CommitAsync(cancellationToken);

        return result;
    }

    private static async Task<CompositionSettings> GetCompositionSettingsAsync(
        FusionArchive archive,
        CancellationToken cancellationToken)
    {
        var compositionSettings = await archive.GetCompositionSettingsAsync(cancellationToken);

        return compositionSettings?.Deserialize(SettingsJsonSerializerContext.Default.CompositionSettings)
            ?? new CompositionSettings
            {
                Merger = new CompositionSettings.MergerSettings
                {
                    EnableGlobalObjectIdentification = false
                }
            };
    }

    private static async Task SaveCompositionSettingsAsync(
        FusionArchive archive,
        SchemaComposerOptions options,
        CancellationToken cancellationToken)
    {
        var settings = new CompositionSettings
        {
            Merger = new CompositionSettings.MergerSettings
            {
                EnableGlobalObjectIdentification = options.Merger.EnableGlobalObjectIdentification
            },
            Satisfiability = new CompositionSettings.SatisfiabilitySettings
            {
                IncludeSatisfiabilityPaths = options.Satisfiability.IncludeSatisfiabilityPaths
            }
        };
        var settingsJson = JsonSerializer.SerializeToDocument(
            settings,
            SettingsJsonSerializerContext.Default.CompositionSettings);

        await archive.SetCompositionSettingsAsync(settingsJson, cancellationToken);
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
