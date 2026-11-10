using System.Collections.Immutable;
using System.Globalization;
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
        Dictionary<string, LocalSourceSchema> localSourceSchemas,
        FusionArchive archive,
        string environment,
        bool preferDevUrls,
        CompositionSettings? compositionSettings,
        JsonDocument? gatewaySettings,
        Stream? legacyArchive,
        CancellationToken cancellationToken)
    {
        if (gatewaySettings is not null
            && (gatewaySettings.RootElement.ValueKind is not JsonValueKind.Object
                || gatewaySettings.RootElement.TryGetProperty("sourceSchemas", out _)))
        {
            compositionLog.Write(LogEntryHelper.InvalidGatewaySettings());
            return (ImmutableArray<CompositionError>)[new("❌ Composition failed")];
        }

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
        foreach (var (newSourceSchemaName, _) in localSourceSchemas)
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

        var allSourceSchemas = new Dictionary<string, (SourceSchemaText Schema, JsonDocument Settings)>(
            localSourceSchemas.Count,
            localSourceSchemas.Comparer);

        foreach (var (schemaName, localSourceSchema) in localSourceSchemas)
        {
            allSourceSchemas[schemaName] = (localSourceSchema.Schema, localSourceSchema.Settings);
        }

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

        var (compositionSettingsRead, existingCompositionSettings) =
            await TryGetCompositionSettingsAsync(archive, compositionLog, cancellationToken);

        if (!compositionSettingsRead)
        {
            return (ImmutableArray<CompositionError>)[new("❌ Composition failed")];
        }

        var mergedCompositionSettings =
            compositionSettings?.MergeInto(existingCompositionSettings) ?? existingCompositionSettings;

        // Report invalid settings through the composition log before the options setter can throw.
        if (!TryValidateMergerSettings(mergedCompositionSettings.Merger, compositionLog))
        {
            return (ImmutableArray<CompositionError>)[new("❌ Composition failed")];
        }

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
                allSourceSchemas.Select(s => s.Value.Schema),
                schemaComposerOptions,
                compositionLog);

            result = schemaComposer.Compose();

            if (result.IsFailure)
            {
                return result;
            }

            var urlOverrides = new Dictionary<string, Uri>(localSourceSchemas.Comparer);

            foreach (var (schemaName, localSourceSchema) in localSourceSchemas)
            {
                if (localSourceSchema.UrlOverride is { } urlOverride)
                {
                    urlOverrides[schemaName] = urlOverride;
                }
            }

            new SettingsComposer().Compose(
                bufferWriter,
                [.. runtimeSourceSchemaSettings],
                environment,
                urlOverrides,
                preferDevUrls,
                compositionLog);
        }
        finally
        {
            foreach (var runtimeSettingsDocument in runtimeSettingsDocuments)
            {
                runtimeSettingsDocument.Dispose();
            }
        }

        var existingMetadata = await archive.GetArchiveMetadataAsync(cancellationToken);
        using var existingGateway = existingMetadata?.SupportedGatewayFormats.IsDefaultOrEmpty == false
            ? await archive.TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion, cancellationToken)
            : null;
        using var composedSettings = JsonDocument.Parse(bufferWriter.WrittenMemory);
        using var mergedGatewaySettings = GatewaySettingsMerger.Merge(
            composedSettings, existingGateway?.Settings, gatewaySettings);

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
            mergedGatewaySettings,
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

    private static bool TryValidateMergerSettings(
        CompositionSettings.MergerSettings mergerSettings,
        ICompositionLog compositionLog)
    {
        if (mergerSettings.DefaultListSize is { } defaultListSize && defaultListSize < 0)
        {
            compositionLog.Write(LogEntryHelper.InvalidDefaultListSizeSettingRange(defaultListSize));
            return false;
        }

        if (mergerSettings.MaxFieldCost is { } maxFieldCost
            && (!double.IsFinite(maxFieldCost) || maxFieldCost < 0))
        {
            compositionLog.Write(
                LogEntryHelper.InvalidMaxFieldCostSetting(
                    maxFieldCost.ToString(CultureInfo.InvariantCulture)));
            return false;
        }

        if (mergerSettings.MaxTypeCost is { } maxTypeCost
            && (!double.IsFinite(maxTypeCost) || maxTypeCost < 0))
        {
            compositionLog.Write(
                LogEntryHelper.InvalidMaxTypeCostSetting(
                    maxTypeCost.ToString(CultureInfo.InvariantCulture)));
            return false;
        }

        if (mergerSettings.MaxResponseSize is { } maxResponseSize
            && (!double.IsFinite(maxResponseSize) || maxResponseSize < 0))
        {
            compositionLog.Write(
                LogEntryHelper.InvalidMaxResponseSizeSetting(
                    maxResponseSize.ToString(CultureInfo.InvariantCulture)));
            return false;
        }

        if (mergerSettings.CostPlanCacheSize is { } costPlanCacheSize && costPlanCacheSize <= 0)
        {
            compositionLog.Write(
                LogEntryHelper.InvalidCostPlanCacheSizeSetting(
                    costPlanCacheSize.ToString(CultureInfo.InvariantCulture)));
            return false;
        }

        return true;
    }

    // Validate the raw setting before deserialization so malformed values become composition errors.
    internal static async Task<(bool Success, CompositionSettings Settings)> TryGetCompositionSettingsAsync(
        FusionArchive archive,
        ICompositionLog compositionLog,
        CancellationToken cancellationToken)
    {
        using var rawCompositionSettings = await archive.GetCompositionSettingsAsync(cancellationToken);

        if (rawCompositionSettings is null)
        {
            return (true, new CompositionSettings());
        }

        var root = rawCompositionSettings.RootElement;

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("merger", out var merger)
            && merger.ValueKind == JsonValueKind.Object
            && merger.TryGetProperty("defaultListSize", out var defaultListSize)
            && defaultListSize.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            && (defaultListSize.ValueKind is not JsonValueKind.Number
                || !defaultListSize.TryGetInt32(out var defaultListSizeInt32)
                || defaultListSizeInt32 < 0))
        {
            // Classify literals without a decimal point as range errors, including exponent notation.
            // Other values receive a type error.
            var logEntry = defaultListSize.ValueKind == JsonValueKind.Number
                && IsWholeNumber(defaultListSize)
                    ? LogEntryHelper.InvalidDefaultListSizeSettingRange(defaultListSize.GetRawText())
                    : LogEntryHelper.InvalidDefaultListSizeSettingType(defaultListSize.GetRawText());

            compositionLog.Write(logEntry);
            return (false, new CompositionSettings());
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("merger", out merger)
            && merger.ValueKind == JsonValueKind.Object
            && !TryValidateRawCostSettings(merger, compositionLog))
        {
            return (false, new CompositionSettings());
        }

        var settings = rawCompositionSettings.Deserialize(SettingsJsonSerializerContext.Default.CompositionSettings)
            ?? new CompositionSettings();
        return (true, settings);
    }

    // Inspect the literal to distinguish unsupported ranges from non-integer syntax.
    // A value such as 1.0 is not an integer literal even though it has no fractional remainder.
    private static bool IsWholeNumber(JsonElement numberElement)
    {
        if (numberElement.TryGetInt64(out _))
        {
            return true;
        }

        var rawText = numberElement.GetRawText();
        var exponentIndex = rawText.IndexOfAny(['e', 'E']);
        var significand = exponentIndex < 0 ? rawText : rawText[..exponentIndex];
        return !significand.Contains('.');
    }

    private static bool TryValidateRawCostSettings(
        JsonElement merger,
        ICompositionLog compositionLog)
    {
        if (HasInvalidNonNegativeFiniteNumber(merger, "maxFieldCost", out var maxFieldCost))
        {
            compositionLog.Write(LogEntryHelper.InvalidMaxFieldCostSetting(maxFieldCost));
            return false;
        }

        if (HasInvalidNonNegativeFiniteNumber(merger, "maxTypeCost", out var maxTypeCost))
        {
            compositionLog.Write(LogEntryHelper.InvalidMaxTypeCostSetting(maxTypeCost));
            return false;
        }

        if (HasInvalidNonNegativeFiniteNumber(merger, "maxResponseSize", out var maxResponseSize))
        {
            compositionLog.Write(LogEntryHelper.InvalidMaxResponseSizeSetting(maxResponseSize));
            return false;
        }

        if (HasInvalidPositiveInt32(merger, "costPlanCacheSize", out var costPlanCacheSize))
        {
            compositionLog.Write(LogEntryHelper.InvalidCostPlanCacheSizeSetting(costPlanCacheSize));
            return false;
        }

        return true;
    }

    private static bool HasInvalidNonNegativeFiniteNumber(
        JsonElement merger,
        string propertyName,
        out string rawValue)
    {
        rawValue = string.Empty;

        if (!merger.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        rawValue = value.GetRawText();
        return value.ValueKind is not JsonValueKind.Number
            || !value.TryGetDouble(out var number)
            || !double.IsFinite(number)
            || number < 0;
    }

    private static bool HasInvalidPositiveInt32(
        JsonElement merger,
        string propertyName,
        out string rawValue)
    {
        rawValue = string.Empty;

        if (!merger.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        rawValue = value.GetRawText();
        return value.ValueKind is not JsonValueKind.Number
            || !value.TryGetInt32(out var number)
            || number <= 0;
    }

    private static async Task SaveCompositionSettingsAsync(
        FusionArchive archive,
        CompositionSettings settings,
        CancellationToken cancellationToken)
    {
        using var settingsJson = JsonSerializer.SerializeToDocument(
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
