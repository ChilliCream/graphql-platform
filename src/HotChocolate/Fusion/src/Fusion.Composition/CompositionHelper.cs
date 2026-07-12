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
        Stream? legacyArchive,
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

                return (ImmutableArray<CompositionError>)[new("❌ Composition failed")];
            }
        }

        var allSourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>(
            sourceSchemas,
            sourceSchemas.Comparer);
        using var carriedSourceSchemaConfigurations =
            new CarriedSourceSchemaConfigurationCollection();

        foreach (var schemaName in existingSourceSchemaNames)
        {
            if (allSourceSchemas.ContainsKey(schemaName))
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

            carriedSourceSchemaConfigurations.Add(configuration);

            var sourceText = await ReadSchemaSourceTextAsync(configuration, cancellationToken);
            var extensionsSourceText = await TryReadSchemaExtensionsTextAsync(configuration, cancellationToken);

            allSourceSchemas[schemaName] = (
                new SourceSchemaText(schemaName, sourceText, extensionsSourceText),
                configuration.Settings);
        }

        var existingCompositionSettings = await GetCompositionSettingsAsync(archive, cancellationToken);
        var mergedCompositionSettings =
            compositionSettings?.MergeInto(existingCompositionSettings) ?? existingCompositionSettings;

        var sourceSchemaOptionsMap = new Dictionary<string, SourceSchemaOptions>();
        var mergerOptions = mergedCompositionSettings.Merger.ToOptions();
        var satisfiabilityOptions = mergedCompositionSettings.Satisfiability.ToOptions();
        var apolloFederationCompatibilityOptions =
            mergedCompositionSettings.ApolloFederationCompatibility.ToOptions();
        var runtimeSourceSchemaSettings = new List<JsonElement>(allSourceSchemas.Count);
        var runtimeSettingsDocuments = new List<JsonDocument>();
        CompositionResult<MutableSchemaDefinition> result;
        using var bufferWriter = new PooledArrayWriter();

        try
        {
            foreach (var (sourceSchemaName, (_, sourceSchemaSettings)) in allSourceSchemas)
            {
                if (!SourceSchemaSettingsReader.TryRead(
                    sourceSchemaName,
                    sourceSchemaSettings,
                    compositionLog,
                    out var settingsResult))
                {
                    return (ImmutableArray<CompositionError>)[new("❌ Composition failed")];
                }

                var sourceSchemaOptions = settingsResult.Options;

                mergedCompositionSettings.Preprocessor?.MergeInto(sourceSchemaOptions.Preprocessor);
                sourceSchemaOptionsMap.Add(sourceSchemaName, sourceSchemaOptions);
                settingsResult.Settings.Satisfiability?.MergeInto(satisfiabilityOptions);

                if (settingsResult.RuntimeSettings is { } runtimeSettings)
                {
                    runtimeSettingsDocuments.Add(runtimeSettings);
                    runtimeSourceSchemaSettings.Add(runtimeSettings.RootElement);
                }
                else
                {
                    runtimeSourceSchemaSettings.Add(sourceSchemaSettings.RootElement);
                }
            }

            var schemaComposerOptions = new SchemaComposerOptions
            {
                SourceSchemas = sourceSchemaOptionsMap,
                Merger = mergerOptions,
                Satisfiability = satisfiabilityOptions,
                ApolloFederationCompatibility = apolloFederationCompatibilityOptions
            };

            var schemaComposer = new SchemaComposer(
                allSourceSchemas.Select(s => s.Value.Item1),
                schemaComposerOptions,
                compositionLog);

            result = schemaComposer.Compose();

            if (result.IsFailure)
            {
                return result;
            }

            new SettingsComposer().Compose(
                bufferWriter,
                [.. runtimeSourceSchemaSettings],
                environment);
        }
        finally
        {
            foreach (var runtimeSettingsDocument in runtimeSettingsDocuments)
            {
                runtimeSettingsDocument.Dispose();
            }
        }

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
            SourceSchemas = [.. allSourceSchemas.Keys]
        };

        await archive.SetArchiveMetadataAsync(metadata, cancellationToken);

        foreach (var (schemaName, (schema, settings)) in allSourceSchemas)
        {
            var schemaExtensions = schema.ExtensionsSourceText is null
                ? default
                : Encoding.UTF8.GetBytes(schema.ExtensionsSourceText);

            await archive.SetSourceSchemaConfigurationAsync(
                schemaName,
                Encoding.UTF8.GetBytes(schema.SourceText),
                settings,
                schemaExtensions,
                cancellationToken);
        }

        await archive.SetGatewayConfigurationAsync(
            result.Value + Environment.NewLine,
            JsonDocument.Parse(bufferWriter.WrittenMemory),
            WellKnownVersions.LatestGatewayFormatVersion,
            cancellationToken);

        await SaveCompositionSettingsAsync(archive, mergedCompositionSettings, cancellationToken);

        if (legacyArchive is not null)
        {
            if (legacyArchive.CanSeek)
            {
                legacyArchive.Position = 0;
            }

            await archive.SetLegacyArchiveFileAsync(legacyArchive, cancellationToken);
        }

        await archive.CommitAsync(cancellationToken);

        return result;
    }

    private static async Task<CompositionSettings> GetCompositionSettingsAsync(
        FusionArchive archive,
        CancellationToken cancellationToken)
    {
        var compositionSettings = await archive.GetCompositionSettingsAsync(cancellationToken);

        return compositionSettings?.Deserialize(SettingsJsonSerializerContext.Default.CompositionSettings)
            ?? new CompositionSettings();
    }

    private static async Task SaveCompositionSettingsAsync(
        FusionArchive archive,
        CompositionSettings settings,
        CancellationToken cancellationToken)
    {
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

    private static async Task<string?> TryReadSchemaExtensionsTextAsync(
        SourceSchemaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await using var stream = await configuration.TryOpenReadSchemaExtensionsAsync(cancellationToken);

        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private sealed class CarriedSourceSchemaConfigurationCollection : IDisposable
    {
        private readonly List<SourceSchemaConfiguration> _configurations = [];

        public void Add(SourceSchemaConfiguration configuration)
            => _configurations.Add(configuration);

        public void Dispose()
        {
            foreach (var configuration in _configurations)
            {
                configuration.Dispose();
            }
        }
    }
}
