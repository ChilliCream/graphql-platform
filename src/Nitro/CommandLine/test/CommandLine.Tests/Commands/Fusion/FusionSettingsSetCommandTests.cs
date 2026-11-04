using System.IO.Compression;
using System.Text.Json;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionSettingsSetCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set a Fusion setting in a Fusion archive.

            Usage:
              nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> [options]

            Arguments:
              <allow-non-resolvable-interface-objects|cache-control-merge-behavior|case-budget|case-budget-exceeded-behavior|cost-plan-cache-size|default-list-size|depth-weight|enable-defer|enable-entity-reuse-splitting|enable-request-grouping|enforce-cost-limits|enum-values-merge-behavior|excess-fanout-weight|exclude-by-tag|fanout-penalty-threshold|global-object-identification|include-satisfiability-paths|inline-unlabeled-defer-fragments|max-expanded-nodes|max-field-cost|max-generated-options-per-work-item|max-planning-time|max-queue-size|max-response-size|max-type-cost|merge-policy|node-resolution|operation-weight|shareable-field-runtime-type-routing|skip-cost-analyzer|tag-merge-behavior>  The name of the setting to change
              <SETTING_VALUE>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                The value to set

            Options:
              -a, --archive <archive> (REQUIRED)      The path to a Fusion archive file [env: NITRO_FUSION_CONFIG_FILE]
              -e, --env, --environment <environment>  The name of the environment used for value substitution in the schema-settings.json files
              --cloud-url <cloud-url>                 The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                     The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                          Show help and usage information

            Example:
              nitro fusion settings set global-object-identification "true" \
                --archive ./gateway.far \
                --env "dev"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "cache-control-merge-behavior",
            "ignore");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--archive' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidSettingName_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "nonexistent-setting",
            "some-value",
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Argument 'nonexistent-setting' not recognized. Must be one of:
            'allow-non-resolvable-interface-objects'
            'cache-control-merge-behavior'
            'case-budget'
            'case-budget-exceeded-behavior'
            'cost-plan-cache-size'
            'default-list-size'
            'enforce-cost-limits'
            'enum-values-merge-behavior'
            'exclude-by-tag'
            'global-object-identification'
            'include-satisfiability-paths'
            'max-field-cost'
            'max-response-size'
            'max-type-cost'
            'node-resolution'
            'shareable-field-runtime-type-routing'
            'skip-cost-analyzer'
            'tag-merge-behavior'
            'depth-weight'
            'operation-weight'
            'excess-fanout-weight'
            'fanout-penalty-threshold'
            'enable-request-grouping'
            'enable-defer'
            'inline-unlabeled-defer-fragments'
            'merge-policy'
            'enable-entity-reuse-splitting'
            'max-planning-time'
            'max-expanded-nodes'
            'max-queue-size'
            'max-generated-options-per-work-item'
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ArchiveDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "cache-control-merge-behavior",
            "ignore",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            Archive file '/some/working/directory/fusion.far' does not exist.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidCacheControlMergeBehavior_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "cache-control-merge-behavior",
            "invalid-value",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            Expected one of the following values for setting 'cache-control-merge-behavior': ignore, include, include-private
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidTagMergeBehavior_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "tag-merge-behavior",
            "invalid-value",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            Expected one of the following values for setting 'tag-merge-behavior': ignore, include, include-private
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidGlobalObjectIdentification_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "global-object-identification",
            "not-a-bool",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            Expected a boolean value for setting 'global-object-identification'.
            """);
    }

    [Theory]
    [InlineData("allow-non-resolvable-interface-objects")]
    [InlineData("include-satisfiability-paths")]
    public async Task Execute_Should_ReturnError_When_BooleanSettingIsInvalid(
        string settingName)
    {
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            settingName,
            "not-a-bool",
            "--archive",
            ArchiveFile);

        result.AssertError($"Expected a boolean value for setting '{settingName}'.");
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    public async Task Execute_Should_ReturnError_When_DefaultListSizeIsInvalid(string settingValue)
    {
        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "default-list-size",
            settingValue,
            "--archive",
            ArchiveFile);

        result.AssertError(
            "Expected a non-negative integer or 'null' for setting 'default-list-size'.");
    }

    [Theory]
    [InlineData(
        "allow-non-resolvable-interface-objects",
        "true",
        "apolloFederationCompatibility",
        "allowNonResolvableInterfaceObjects",
        "true")]
    [InlineData(
        "allow-non-resolvable-interface-objects",
        "false",
        "apolloFederationCompatibility",
        "allowNonResolvableInterfaceObjects",
        "false")]
    [InlineData(
        "cache-control-merge-behavior",
        "ignore",
        "merger",
        "cacheControlMergeBehavior",
        "\"Ignore\"")]
    [InlineData(
        "default-list-size",
        "5",
        "merger",
        "defaultListSize",
        "5")]
    [InlineData(
        "enum-values-merge-behavior",
        "union",
        "merger",
        "enumValuesMergeBehavior",
        "\"Union\"")]
    [InlineData(
        "exclude-by-tag",
        "internal,private",
        "preprocessor",
        "excludeByTag",
        "[\n      \"internal\",\n      \"private\"\n    ]")]
    [InlineData(
        "global-object-identification",
        "false",
        "merger",
        "enableGlobalObjectIdentification",
        "false")]
    [InlineData(
        "include-satisfiability-paths",
        "true",
        "satisfiability",
        "includeSatisfiabilityPaths",
        "true")]
    [InlineData(
        "node-resolution",
        "gateway",
        "merger",
        "nodeResolution",
        "\"Gateway\"")]
    [InlineData(
        "shareable-field-runtime-type-routing",
        "common-runtime-types",
        "apolloFederationCompatibility",
        "shareableFieldRuntimeTypeRouting",
        "\"CommonRuntimeTypes\"")]
    [InlineData(
        "tag-merge-behavior",
        "include-private",
        "merger",
        "tagMergeBehavior",
        "\"IncludePrivate\"")]
    public async Task Execute_Should_PersistEveryUserFacingCompositionSetting(
        string settingName,
        string settingValue,
        string sectionName,
        string propertyName,
        string expectedJson)
    {
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));

        try
        {
            var result = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                settingName,
                settingValue,
                "--archive",
                archiveFile);

            Assert.Equal(0, result.ExitCode);
            using var archive = FusionArchive.Open(archiveFile);
            using var settings = await archive.GetCompositionSettingsAsync(
                TestContext.Current.CancellationToken);
            Assert.NotNull(settings);
            Assert.Equal(
                expectedJson,
                settings.RootElement
                    .GetProperty(sectionName)
                    .GetProperty(propertyName)
                    .GetRawText());
        }
        finally
        {
            File.Delete(archiveFile);
        }
    }

    [Theory]
    [InlineData("max-field-cost", "2000", "cost", "maxFieldCost", "2000")]
    [InlineData("max-type-cost", "-1", "cost", "maxTypeCost", "-1")]
    [InlineData("enforce-cost-limits", "true", "cost", "enforceCostLimits", "true")]
    [InlineData("skip-cost-analyzer", "false", "cost", "skipAnalyzer", "false")]
    [InlineData("max-response-size", "2.5", "cost", "maxResponseSize", "2.5")]
    [InlineData("cost-plan-cache-size", "0", "cost", "costPlanCacheSize", "0")]
    [InlineData("case-budget", "-1", "cost", "caseBudget", "-1")]
    [InlineData("case-budget-exceeded-behavior", "evaluate-per-request", "cost", "caseBudgetExceededBehavior", "\"EvaluatePerRequest\"")]
    [InlineData("depth-weight", "15.5", "planner", "depthWeight", "15.5")]
    [InlineData("operation-weight", "1.5", "planner", "operationWeight", "1.5")]
    [InlineData("excess-fanout-weight", "3", "planner", "excessFanoutWeight", "3")]
    [InlineData("fanout-penalty-threshold", "8", "planner", "fanoutPenaltyThreshold", "8")]
    [InlineData("enable-request-grouping", "false", "planner", "enableRequestGrouping", "false")]
    [InlineData("enable-defer", "false", "planner", "enableDefer", "false")]
    [InlineData("inline-unlabeled-defer-fragments", "false", "planner", "inlineUnlabeledDeferFragments", "false")]
    [InlineData("merge-policy", "aggressive", "planner", "mergePolicy", "\"Aggressive\"")]
    [InlineData("enable-entity-reuse-splitting", "false", "planner", "enableEntityReuseSplitting", "false")]
    [InlineData("max-planning-time", "00:00:05", "planner", "maxPlanningTime", "\"00:00:05\"")]
    [InlineData("max-expanded-nodes", "100", "planner", "maxExpandedNodes", "100")]
    [InlineData("max-queue-size", "100", "planner", "maxQueueSize", "100")]
    [InlineData("max-generated-options-per-work-item", "64", "planner", "maxGeneratedOptionsPerWorkItem", "64")]
    public async Task Execute_Should_WriteGatewaySetting(
        string name, string value, string section, string property, string expected)
    {
        // arrange
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"), file);
        SetupFile(file, new MemoryStream(File.ReadAllBytes(file)));
        try
        {
            // act
            var result = await ExecuteCommandAsync("fusion", "settings", "set", name, value, "--archive", file);

            // assert
            Assert.True(result.ExitCode == 0, result.StdErr + result.StdOut);
            using var archive = FusionArchive.Open(file);
            using var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion, TestContext.Current.CancellationToken);
            Assert.Equal(expected, config!.Settings.RootElement.GetProperty(section).GetProperty(property).GetRawText());
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData("case-budget-exceeded-behavior")]
    [InlineData("merge-policy")]
    public async Task Execute_Should_RejectInvalidEnum(string name)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "settings", "set", name, "invalid", "--archive", ArchiveFile);

        // assert
        result.AssertError($"Invalid {(name == "merge-policy" ? "merge" : "case")} value for setting '{name}'.");
    }

    [Fact]
    public async Task Execute_Should_PreserveGatewaySettingsAndSchema_When_SettingCostAndPlanner()
    {
        // arrange
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"), file);
        SetupFile(file, new MemoryStream(File.ReadAllBytes(file)));
        try
        {
            string originalSchema;
            using (var archive = FusionArchive.Open(file, mode: FusionArchiveMode.Update))
            {
                using var config = await archive.TryGetGatewayConfigurationAsync(
                    WellKnownVersions.LatestGatewayFormatVersion, TestContext.Current.CancellationToken);
                await using (var stream = await config!.OpenReadSchemaAsync(TestContext.Current.CancellationToken))
                using (var reader = new StreamReader(stream))
                {
                    originalSchema = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
                }
                using var settings = JsonDocument.Parse("""
                    { "sourceSchemas": [{ "name": "products" }], "other": { "keep": true },
                      "cost": { "maxFieldCost": 1, "skipAnalyzer": true },
                      "planner": { "mergePolicy": "None", "enableDefer": false } }
                    """);
                await archive.SetGatewayConfigurationAsync(originalSchema, settings, config.Version, TestContext.Current.CancellationToken);
                await archive.CommitAsync(TestContext.Current.CancellationToken);
            }

            // act
            var cost = await ExecuteCommandAsync("fusion", "settings", "set", "max-field-cost", "2000", "--archive", file);
            var planner = await ExecuteCommandAsync("fusion", "settings", "set", "merge-policy", "aggressive", "--archive", file);

            // assert
            Assert.True(cost.ExitCode == 0, cost.StdErr);
            Assert.True(planner.ExitCode == 0, planner.StdErr);
            using var updatedArchive = FusionArchive.Open(file);
            using var updated = await updatedArchive.TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion, TestContext.Current.CancellationToken);
            Assert.Equal(originalSchema, await GetFusionSchemaAsync(updatedArchive));
            Snapshot.Create()
                .Add(updated!.Settings.RootElement, "Gateway settings")
                .MatchMarkdownSnapshot();
        }
        finally
        {
            File.Delete(file);
        }
    }
    [Theory]
    [InlineData("max-field-cost", "number")]
    [InlineData("merge-policy", "merge")]
    public async Task Execute_Should_RejectNullRuntimeValue(string name, string type)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "settings", "set", name, "null", "--archive", ArchiveFile);

        // assert
        result.AssertError($"Invalid {type} value for setting '{name}'.");
    }
    [Fact]
    public async Task Execute_Should_RefuseSignedArchive()
    {
        // arrange
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"), file);
        AddSignatureManifest(file);
        SetupFile(file, new MemoryStream(File.ReadAllBytes(file)));
        try
        {
            // act
            var result = await ExecuteCommandAsync("fusion", "settings", "set", "max-field-cost", "2000", "--archive", file);

            // assert
            result.AssertError("The archive is signed. Changing gateway settings would invalidate its signature.");
        }
        finally
        {
            File.Delete(file);
        }
    }
    private static void AddSignatureManifest(string file)
    {
        using var zip = ZipFile.Open(file, ZipArchiveMode.Update);
        zip.CreateEntry(".signature/manifest.json");
    }

    [Fact]
    public async Task Execute_Should_SetAndUnset_DefaultListSize()
    {
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));

        try
        {
            var setResult = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                "default-list-size",
                "5",
                "--archive",
                archiveFile);

            Assert.Equal(0, setResult.ExitCode);

            using (var archive = FusionArchive.Open(archiveFile))
            {
                using var settings = await archive.GetCompositionSettingsAsync(
                    TestContext.Current.CancellationToken);
                Assert.NotNull(settings);
                Assert.Equal(
                    "5",
                    settings.RootElement
                        .GetProperty("merger")
                        .GetProperty("defaultListSize")
                        .GetRawText());
            }

            var unsetResult = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                "default-list-size",
                "null",
                "--archive",
                archiveFile);

            Assert.Equal(0, unsetResult.ExitCode);

            using (var archive = FusionArchive.Open(archiveFile))
            {
                using var settings = await archive.GetCompositionSettingsAsync(
                    TestContext.Current.CancellationToken);
                Assert.NotNull(settings);
                Assert.Equal(
                    "null",
                    settings.RootElement
                        .GetProperty("merger")
                        .GetProperty("defaultListSize")
                        .GetRawText());
            }
        }
        finally
        {
            File.Delete(archiveFile);
        }
    }

    [Theory]
    [InlineData("9999999999")]
    [InlineData("-1")]
    public async Task Execute_Should_ReturnError_When_UnsettingAlreadyInvalidPersistedDefaultListSize(
        string persistedRawValue)
    {
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));

        try
        {
            using (var archive = FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update))
            {
                using var invalidCompositionSettings =
                    JsonDocument.Parse($$"""{ "merger": { "defaultListSize": {{persistedRawValue}} } }""");
                await archive.SetCompositionSettingsAsync(
                    invalidCompositionSettings,
                    TestContext.Current.CancellationToken);
                await archive.CommitAsync(TestContext.Current.CancellationToken);
            }

            var result = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                "default-list-size",
                "null",
                "--archive",
                archiveFile);

            result.AssertError(
                "The 'defaultListSize' composition setting must be a non-negative integer "
                + $"no larger than 2147483647 ({persistedRawValue}).");
        }
        finally
        {
            File.Delete(archiveFile);
        }
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Execute_Should_ReturnError_When_NodeResolutionIsInvalid(InteractionMode mode)
    {
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "node-resolution",
            "invalid-value",
            "--archive",
            ArchiveFile);

        result.AssertError(
            """
            Expected one of the following values for setting 'node-resolution': gateway, source-schema
            """);
    }

    [Fact]
    public async Task Execute_Should_ReturnSuccess_When_NodeResolutionIsGateway()
    {
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));

        try
        {
            var result = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                "node-resolution",
                "gateway",
                "--archive",
                archiveFile);

            Assert.True(
                result.ExitCode == 0,
                $"Standard output:{Environment.NewLine}{result.StdOut}{Environment.NewLine}"
                + $"Standard error:{Environment.NewLine}{result.StdErr}");
        }
        finally
        {
            File.Delete(archiveFile);
        }
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Execute_Should_ReturnError_When_ShareableFieldRuntimeTypeRoutingIsInvalid(
        InteractionMode mode)
    {
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "fusion",
            "settings",
            "set",
            "shareable-field-runtime-type-routing",
            "invalid-value",
            "--archive",
            ArchiveFile);

        result.AssertError(
            """
            Expected one of the following values for setting 'shareable-field-runtime-type-routing': source-local, common-runtime-types
            """);
    }

    [Theory]
    [InlineData("source-local", "SourceLocal")]
    [InlineData("common-runtime-types", "CommonRuntimeTypes")]
    public async Task Execute_Should_PersistShareableFieldRuntimeTypeRouting(
        string value,
        string expectedValue)
    {
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));

        try
        {
            var result = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                "shareable-field-runtime-type-routing",
                value,
                "--archive",
                archiveFile);

            Assert.Equal(0, result.ExitCode);
            using var archive = FusionArchive.Open(archiveFile);
            var settings = await archive.GetCompositionSettingsAsync(
                TestContext.Current.CancellationToken);
            Assert.NotNull(settings);
            Assert.Equal(
                expectedValue,
                settings.RootElement
                    .GetProperty("apolloFederationCompatibility")
                    .GetProperty("shareableFieldRuntimeTypeRouting")
                    .GetString());
        }
        finally
        {
            File.Delete(archiveFile);
        }
    }
}
