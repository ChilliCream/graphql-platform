#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.Settings;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionSettingsSetCommand : Command
{
    public FusionSettingsSetCommand() : base("set")
    {
        Description = "Set a Fusion setting in a Fusion archive.";

        Arguments.Add(Opt<FusionSettingsNameArgument>.Instance);
        Arguments.Add(Opt<FusionSettingsValueArgument>.Instance);

        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        Options.Add(Opt<FusionEnvironmentOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion settings set global-object-identification "true" \
              --archive ./gateway.far \
              --env "dev"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();

        var settingName = parseResult.GetRequiredValue(Opt<FusionSettingsNameArgument>.Instance);
        var settingValue = parseResult.GetRequiredValue(Opt<FusionSettingsValueArgument>.Instance);
        var archiveFile = parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);
        var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);

        if (GetGatewaySetting(settingName) is { } gatewaySetting)
        {
            return await SetGatewaySettingAsync(
                fileSystem, archiveFile, settingName, settingValue, gatewaySetting, cancellationToken);
        }

        var compositionSettings = new CompositionSettings();
        Action<CompositionSettings>? clearSetting = null;

        switch (settingName)
        {
            case FusionSettingsNameArgument.AllowNonResolvableInterfaceObjects:
                if (!bool.TryParse(settingValue, out var allowNonResolvableInterfaceObjects))
                {
                    throw new ExitException($"Expected a boolean value for setting '{settingName}'.");
                }

                compositionSettings.ApolloFederationCompatibility
                    .AllowNonResolvableInterfaceObjects = allowNonResolvableInterfaceObjects;
                break;

            case FusionSettingsNameArgument.CacheControlMergeBehavior:
                if (!DirectiveMergeBehaviorParser.TryParse(
                    settingValue,
                    out var cacheControlMergeBehavior))
                {
                    throw new ExitException(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorParser.Values)}");
                }

                compositionSettings.Merger.CacheControlMergeBehavior = cacheControlMergeBehavior;
                break;

            case FusionSettingsNameArgument.DefaultListSize:
                if (string.Equals(settingValue, "null", StringComparison.Ordinal))
                {
                    clearSetting = settings => settings.Merger.DefaultListSize = null;
                }
                else if (!int.TryParse(
                        settingValue,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var defaultListSize)
                    || defaultListSize < 0)
                {
                    throw new ExitException(
                        $"Expected a non-negative integer or 'null' for setting '{settingName}'.");
                }
                else
                {
                    compositionSettings.Merger.DefaultListSize = defaultListSize;
                }
                break;

            case FusionSettingsNameArgument.EnumValuesMergeBehavior:
                if (!EnumValuesMergeBehaviorParser.TryParse(settingValue, out var enumValuesMergeBehavior))
                {
                    throw new ExitException(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", EnumValuesMergeBehaviorParser.Values)}");
                }

                compositionSettings.Merger.EnumValuesMergeBehavior = enumValuesMergeBehavior;
                break;

            case FusionSettingsNameArgument.ExcludeByTag:
                var tags = settingValue.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries);

                compositionSettings.Preprocessor.ExcludeByTag = tags.ToHashSet();
                break;

            case FusionSettingsNameArgument.GlobalObjectIdentification:
                if (!bool.TryParse(settingValue, out var enableGlobalObjectIdentification))
                {
                    throw new ExitException($"Expected a boolean value for setting '{settingName}'.");
                }

                compositionSettings.Merger.EnableGlobalObjectIdentification = enableGlobalObjectIdentification;
                break;

            case FusionSettingsNameArgument.IncludeSatisfiabilityPaths:
                if (!bool.TryParse(settingValue, out var includeSatisfiabilityPaths))
                {
                    throw new ExitException($"Expected a boolean value for setting '{settingName}'.");
                }

                compositionSettings.Satisfiability.IncludeSatisfiabilityPaths =
                    includeSatisfiabilityPaths;
                break;

            case FusionSettingsNameArgument.NodeResolution:
                compositionSettings.Merger.NodeResolution = settingValue switch
                {
                    "gateway" => NodeResolution.Gateway,
                    "source-schema" => NodeResolution.SourceSchema,
                    _ => throw new ExitException(
                        $"Expected one of the following values for setting '{settingName}': "
                        + "gateway, source-schema")
                };
                break;

            case FusionSettingsNameArgument.ShareableFieldRuntimeTypeRouting:
                compositionSettings.ApolloFederationCompatibility
                    .ShareableFieldRuntimeTypeRouting = settingValue switch
                    {
                        "source-local" => ShareableFieldRuntimeTypeRouting.SourceLocal,
                        "common-runtime-types" =>
                            ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
                        _ => throw new ExitException(
                            $"Expected one of the following values for setting '{settingName}': "
                            + "source-local, common-runtime-types")
                    };
                break;

            case FusionSettingsNameArgument.TagMergeBehavior:
                if (!DirectiveMergeBehaviorParser.TryParse(
                    settingValue,
                    out var tagMergeBehavior))
                {
                    throw new ExitException(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorParser.Values)}");
                }

                compositionSettings.Merger.TagMergeBehavior = tagMergeBehavior;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(settingName));
        }

        if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
        }

        if (!fileSystem.FileExists(archiveFile))
        {
            throw new ExitException(Messages.ArchiveFileDoesNotExist(archiveFile));
        }

        using var archive = FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update);

        if (clearSetting is not null)
        {
            // Clear the persisted value first: merging null settings preserves an existing value.
            await ClearSettingAsync(archive, clearSetting, cancellationToken);
        }

        environment ??= environmentVariables.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        await using var composeActivity = console.StartActivity(
            "Composing new configuration",
            "Failed to compose new configuration.");

        var (result, compositionLog) = await FusionPublishHelpers.ComposeAsync(
            archive,
            environment,
            [],
            compositionSettings,
            legacyArchive: null,
            cancellationToken);

        if (result.IsSuccess)
        {
            composeActivity.Success("Composed new configuration.");

            return ExitCodes.Success;
        }
        else
        {
            await composeActivity.FailAllAsync();

            console.WriteLine();
            console.WriteLine("## Composition log");
            console.WriteLine();

            FusionComposeCommand.WriteCompositionLog(
                compositionLog,
                console.Out,
                false);

            foreach (var error in result.Errors)
            {
                console.Error.WriteErrorLine(error.Message);
            }

            throw new ExitException();
        }
    }

    private static (string Section, string Property, string Type)? GetGatewaySetting(string name)
        => name switch
        {
            FusionSettingsNameArgument.MaxFieldCost => ("cost", "maxFieldCost", "number"),
            FusionSettingsNameArgument.MaxTypeCost => ("cost", "maxTypeCost", "number"),
            FusionSettingsNameArgument.EnforceCostLimits => ("cost", "enforceCostLimits", "boolean"),
            FusionSettingsNameArgument.SkipCostAnalyzer => ("cost", "skipAnalyzer", "boolean"),
            FusionSettingsNameArgument.MaxResponseSize => ("cost", "maxResponseSize", "number"),
            FusionSettingsNameArgument.CostPlanCacheSize => ("cost", "costPlanCacheSize", "integer"),
            FusionSettingsNameArgument.CaseBudget => ("cost", "caseBudget", "integer"),
            FusionSettingsNameArgument.CaseBudgetExceededBehavior => ("cost", "caseBudgetExceededBehavior", "case"),
            FusionSettingsNameArgument.DepthWeight => ("planner", "depthWeight", "number"),
            FusionSettingsNameArgument.OperationWeight => ("planner", "operationWeight", "number"),
            FusionSettingsNameArgument.ExcessFanoutWeight => ("planner", "excessFanoutWeight", "number"),
            FusionSettingsNameArgument.FanoutPenaltyThreshold => ("planner", "fanoutPenaltyThreshold", "integer"),
            FusionSettingsNameArgument.EnableRequestGrouping => ("planner", "enableRequestGrouping", "boolean"),
            FusionSettingsNameArgument.EnableDefer => ("planner", "enableDefer", "boolean"),
            FusionSettingsNameArgument.InlineUnlabeledDeferFragments => ("planner", "inlineUnlabeledDeferFragments", "boolean"),
            FusionSettingsNameArgument.MergePolicy => ("planner", "mergePolicy", "merge"),
            FusionSettingsNameArgument.EnableEntityReuseSplitting => ("planner", "enableEntityReuseSplitting", "boolean"),
            FusionSettingsNameArgument.MaxPlanningTime => ("planner", "maxPlanningTime", "time"),
            FusionSettingsNameArgument.MaxExpandedNodes => ("planner", "maxExpandedNodes", "integer"),
            FusionSettingsNameArgument.MaxQueueSize => ("planner", "maxQueueSize", "integer"),
            FusionSettingsNameArgument.MaxGeneratedOptionsPerWorkItem => ("planner", "maxGeneratedOptionsPerWorkItem", "integer"),
            _ => null
        };

    private static async Task<int> SetGatewaySettingAsync(
        IFileSystem fileSystem,
        string archiveFile,
        string name,
        string value,
        (string Section, string Property, string Type) setting,
        CancellationToken cancellationToken)
    {
        if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
        }

        if (!fileSystem.FileExists(archiveFile))
        {
            throw ThrowHelper.Exit(Messages.ArchiveFileDoesNotExist(archiveFile));
        }

        using var archive = FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update);
        if (archive.IsSigned)
        {
            throw ThrowHelper.Exit("The archive is signed. Changing gateway settings would invalidate its signature.");
        }

        JsonNode? parsed = value switch
        {
            _ when setting.Type == "boolean" && bool.TryParse(value, out var boolean) => JsonValue.Create(boolean),
            _ when setting.Type == "integer" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) => JsonValue.Create(integer),
            _ when setting.Type == "number" && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) => JsonValue.Create(number),
            _ when setting.Type == "case" && CaseBudgetExceededBehaviorParser.TryParse(value, out var behavior) => JsonValue.Create(behavior.ToString()),
            _ when setting.Type == "merge" && value is "none" or "conservative" or "balanced" or "aggressive" => JsonValue.Create(char.ToUpperInvariant(value[0]) + value[1..]),
            _ when setting.Type == "time" && TimeSpan.TryParseExact(value, "c", CultureInfo.InvariantCulture, out var time) => JsonValue.Create(time.ToString("c", CultureInfo.InvariantCulture)),
            _ => throw ThrowHelper.Exit($"Invalid {setting.Type} value for setting '{name}'.")
        };

        using var configuration = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion, cancellationToken)
            ?? throw ThrowHelper.Exit("The archive does not contain a supported gateway configuration.");
        var settings = JsonNode.Parse(configuration.Settings.RootElement.GetRawText())!.AsObject();
        if (settings[setting.Section] is null)
        {
            settings[setting.Section] = new JsonObject();
        }
        settings[setting.Section]![setting.Property] = parsed;
        using var updatedSettings = JsonDocument.Parse(settings.ToJsonString());
        await using var buffer = new MemoryStream();
        await using (var schema = await configuration.OpenReadSchemaAsync(cancellationToken))
        {
            await schema.CopyToAsync(buffer, cancellationToken);
        }
        await archive.SetGatewayConfigurationAsync(buffer.ToArray(), updatedSettings, configuration.Version, cancellationToken);
        await archive.CommitAsync(cancellationToken);
        return ExitCodes.Success;
    }
    private static async Task ClearSettingAsync(
        FusionArchive archive,
        Action<CompositionSettings> clearSetting,
        CancellationToken cancellationToken)
    {
        var compositionLog = new CompositionLog();

        // Validate raw settings so invalid values produce the same composition errors as ComposeAsync.
        var (settingsRead, existingSettings) = await CompositionHelper.TryGetCompositionSettingsAsync(
            archive,
            compositionLog,
            cancellationToken);

        if (!settingsRead)
        {
            throw new ExitException(string.Join(Environment.NewLine, compositionLog.Select(e => e.Message)));
        }

        clearSetting(existingSettings);

        using var updatedSettingsJson = JsonSerializer.SerializeToDocument(
            existingSettings,
            SettingsJsonSerializerContext.Default.CompositionSettings);

        await archive.SetCompositionSettingsAsync(updatedSettingsJson, cancellationToken);
    }
}
