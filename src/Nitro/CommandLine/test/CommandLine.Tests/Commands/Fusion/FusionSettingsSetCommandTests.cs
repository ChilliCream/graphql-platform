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
              Set a Fusion composition setting in a Fusion archive.

            Usage:
              nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> [options]

            Arguments:
              <allow-non-resolvable-interface-objects|cache-control-merge-behavior|exclude-by-tag|global-object-identification|include-satisfiability-paths|node-resolution|shareable-field-runtime-type-routing|tag-merge-behavior>  The name of the setting to change
              <SETTING_VALUE>                                                                                                                                                                                                         The value to set

            Options:
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -e, --env, --environment <environment>               The name of the environment used for value substitution in the schema-settings.json files
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                                  The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information

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
            'exclude-by-tag'
            'global-object-identification'
            'include-satisfiability-paths'
            'node-resolution'
            'shareable-field-runtime-type-routing'
            'tag-merge-behavior'
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
            Expected one of the following values for setting 'node-resolution': router, gateway, source-schema
            """);
    }

    [Theory]
    [InlineData("router", "Gateway", InteractionMode.Interactive)]
    [InlineData("gateway", "Gateway", InteractionMode.Interactive)]
    [InlineData("source-schema", "SourceSchema", InteractionMode.Interactive)]
    [InlineData("router", "Gateway", InteractionMode.NonInteractive)]
    [InlineData("gateway", "Gateway", InteractionMode.NonInteractive)]
    [InlineData("source-schema", "SourceSchema", InteractionMode.NonInteractive)]
    [InlineData("router", "Gateway", InteractionMode.JsonOutput)]
    [InlineData("gateway", "Gateway", InteractionMode.JsonOutput)]
    [InlineData("source-schema", "SourceSchema", InteractionMode.JsonOutput)]
    public async Task Execute_Should_PreserveSerializedSettings_When_NodeResolutionIsProvided(
        string value,
        string persistedValue,
        InteractionMode mode)
    {
        SetupInteractionMode(mode);
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        SetupFile("schema.graphqls", "type Query { field: String }");
        SetupFile("schema-settings.json",
            """{ "name": "Test", "transports": { "http": { "url": "http://localhost/graphql" } } }""");

        try
        {
            var compose = await ExecuteCommandAsync(
                "fusion", "compose", "--source-schema-file", "schema.graphqls",
                "--enable-global-object-identification", "--node-resolution",
                value == "source-schema" ? "router" : "source-schema",
                "--archive", archiveFile);
            compose.AssertSuccess();
            SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));

            var result = await ExecuteCommandAsync(
                "fusion",
                "settings",
                "set",
                "node-resolution",
                value,
                "--archive",
                archiveFile);

            result.AssertSuccess();
            using var archive = FusionArchive.Open(archiveFile);
            using var settings = await archive.GetCompositionSettingsAsync(
                TestContext.Current.CancellationToken);
            Snapshot.Create(persistedValue)
                .Add(settings, "Composition settings")
                .MatchMarkdownSnapshot();
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
