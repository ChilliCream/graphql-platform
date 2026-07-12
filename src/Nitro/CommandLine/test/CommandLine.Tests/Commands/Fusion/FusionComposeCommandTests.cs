using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionComposeCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture), IDisposable
{
    private readonly List<string> _tempFiles = [];

    private static readonly string s_resourcesDir =
        Path.GetFullPath("__resources__");

    private static readonly string s_validExample1CompositeSchema =
        File.ReadAllText("__resources__/valid-example-1-result/composite-schema.graphqls");

    private static readonly string s_validExcludeByTagCompositeSchema =
        File.ReadAllText("__resources__/valid-exclude-by-tag-result/composite-schema.graphqls");

    private static readonly string s_validExtensionsCompositeSchema =
        File.ReadAllText("__resources__/valid-extensions-result/composite-schema.graphqls");

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Compose multiple source schemas into a single composite schema.

            Usage:
              nitro fusion compose [options]

            Options:
              -f, --source-schema-file <source-schema-file>                               One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              --source-schema-url <url> <settings-file>                                   A source schema URL followed by its source schema settings file
              -a, --archive, --configuration <archive>                                    The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -e, --env, --environment <environment>                                      The name of the environment used for value substitution in the schema-settings.json files
              --cache-control-merge-behavior <ignore|include|include-private>             Choose how @cacheControl directives are merged
              --enable-global-object-identification                                       Add the 'Query.node' field for global object identification
              --node-resolution <gateway|source-schema>                                   Choose whether Query.node identifiers are resolved by the gateway or a source schema
              --tag-merge-behavior <ignore|include|include-private>                       Choose how @tag directives are merged
              --shareable-field-runtime-type-routing <common-runtime-types|source-local>  Choose how runtime types are routed for Apollo Federation shareable abstract fields
              --allow-non-resolvable-interface-objects                                    Allow Apollo Federation interface objects without a resolvable key
              --include-satisfiability-paths                                              Include paths in satisfiability error messages
              --watch                                                                     Watch for file changes and recompose automatically
              -w, --working-directory <working-directory>                                 Set the working directory for the command
              --exclude-by-tag <exclude-by-tag>                                           One or more tags to exclude from the composition
              --remove-source-schema <remove-source-schema>                               One or more source schemas to remove from the archive before composing.
              --cloud-url <cloud-url>                                                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                                                         The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                              Show help and usage information

            Example:
              nitro fusion compose \
                --source-schema-file ./products/schema.graphqls \
                --source-schema-file ./reviews/schema.graphqls \
                --archive ./gateway.far \
                --env "dev"
            """);
    }

    [Fact]
    public async Task Compose_RemoveSourceSchema_RecomposesArchive()
    {
        // arrange
        var archiveFileName = await BuildArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--archive",
            archiveFileName,
            "--remove-source-schema",
            "Schema2");

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var names = await archive.GetSourceSchemaNamesAsync(TestContext.Current.CancellationToken);
        var schema = await GetFusionSchemaAsync(archive);
        Assert.Equal(["Schema1"], names);
        Assert.Contains("schema1Field", schema);
        Assert.DoesNotContain("schema2Field", schema);
    }

    [Fact]
    public async Task Compose_RenameReplaceSourceSchema_RecomposesArchive()
    {
        // arrange
        var archiveFileName = await BuildArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        SetupReplacementSchema();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--archive",
            archiveFileName,
            "--remove-source-schema",
            "Schema2",
            "--source-schema-file",
            "replacement/schema.graphqls");

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var names = await archive.GetSourceSchemaNamesAsync(TestContext.Current.CancellationToken);
        var schema = await GetFusionSchemaAsync(archive);
        Assert.Equal(["Schema1", "Schema3"], names);
        Assert.Contains("schema1Field", schema);
        Assert.Contains("schema3Field", schema);
        Assert.DoesNotContain("schema2Field", schema);
    }

    [Fact]
    public async Task Compose_RemoveMissingSourceSchema_ReturnsError()
    {
        // arrange
        var archiveFileName = await BuildArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--archive",
            archiveFileName,
            "--remove-source-schema",
            "DoesNotExist");

        // assert
        result.AssertError(
            $"Source schema 'DoesNotExist' does not exist in the Fusion archive '{archiveFileName}'.");
    }

    [Fact]
    public async Task Compose_RemoveMissingSourceSchema_LeavesArchiveUnchanged()
    {
        // arrange
        var archiveFileName = await BuildArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        var before = await File.ReadAllBytesAsync(
            archiveFileName,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--archive",
            archiveFileName,
            "--remove-source-schema",
            "DoesNotExist");

        // assert
        var after = await File.ReadAllBytesAsync(
            archiveFileName,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Compose_RemoveOnly_DoesNotAutoDiscoverWorkingDirectorySchemas()
    {
        // arrange
        var archiveFileName = await BuildArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        var workDir = Path.Combine(s_resourcesDir, "valid-example-1");
        SetupWorkingDirectoryWithSchemas(
            workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--archive",
            archiveFileName,
            "--working-directory",
            workDir,
            "--remove-source-schema",
            "Schema2");

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var names = await archive.GetSourceSchemaNamesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Schema1"], names);
    }

    [Fact]
    public async Task Compose_RemoveSourceSchemaWithWatch_ReturnsError()
    {
        // arrange
        var archiveFileName = await BuildArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--archive",
            archiveFileName,
            "--remove-source-schema",
            "Schema2",
            "--watch");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "The '--remove-source-schema' and '--watch' options cannot be combined.",
            result.StdErr);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToStdOut()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToStdOut()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var workDir = Path.Combine(s_resourcesDir, "valid-example-1");
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInCurrentDirectory()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileRelativeToCurrentDirectory()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileInWorkingDirectory()
    {
        // arrange
        var workDir = Path.Combine(s_resourcesDir, "valid-example-1");
        var archiveFileName = CreateTempFile();
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileRelativeToWorkingDirectory()
    {
        // arrange
        var workDir = Path.Combine(s_resourcesDir, "valid-example-1");
        var archiveFileName = CreateTempFile();
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileAtFullyQualifiedPath()
    {
        // arrange
        var workDir = Path.Combine(s_resourcesDir, "valid-example-1");
        var archiveFileName = CreateTempFile();
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInNonExistentWorkingDirectory()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            "non-existent",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"));

        // assert
        Assert.Equal(1, result.ExitCode);
        result.StdErr.MatchInlineSnapshot(
            """
            ❌ Working directory 'non-existent' does not exist.
            """);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInNewDirectory()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_FromNonExistentFiles()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        const string nonExistentFile = "/path/to/non-existent-1.graphqls";

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            nonExistentFile,
            "--archive",
            archiveFileName);

        // assert
        // The console wraps long paths across lines, so normalize before matching
        var stderr = result.StdErr.Replace("\n", "").Replace("\r", "");

        stderr.MatchInlineSnapshot(
            """
            Schema file '/path/to/non-existent-1.graphqls' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Compose_InvalidExample1_FromWorkingDirectory_ToStdOutWithWarnings()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var workDir = Path.Combine(s_resourcesDir, "invalid-example-1");
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        result = result with { StdOut = result.StdOut.Replace(archiveFileName, "/path/to/archive-file.far") };

        result.AssertSuccess(
            """
            ⚠️ [WRN] The lookup field 'Query.userById' in schema 'Schema1' should return a nullable type. (LOOKUP_RETURNS_NON_NULLABLE_TYPE)
            ⚠️ [WRN] The lookup field 'Query.userById' in schema 'Schema2' should return a nullable type. (LOOKUP_RETURNS_NON_NULLABLE_TYPE)

            ✅ Composite schema written to '/path/to/archive-file.far'.
            """);
    }

    [Fact]
    public async Task Compose_InvalidExample2_FromWorkingDirectory_ToStdOutWithWarningsAndErrors()
    {
        // arrange
        var workDir = Path.Combine(s_resourcesDir, "invalid-example-2");
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir);

        // assert
        Assert.Equal(1, result.ExitCode);
        result.StdOut.MatchInlineSnapshot(
            """
            ⚠️ [WRN] The lookup field 'Query.userById' in schema 'Schema1' should return a nullable type. (LOOKUP_RETURNS_NON_NULLABLE_TYPE)
            ❌ [ERR] The @provides directive on field 'Query.userById' in schema 'Schema1' specifies an invalid field selection. (PROVIDES_INVALID_FIELDS)
               - The field 'username' does not exist on the type 'User'.
               - The field 'email' does not exist on the type 'User'.
            ⚠️ [WRN] The lookup field 'Query.userByUsername' in schema 'Schema2' should return a nullable type. (LOOKUP_RETURNS_NON_NULLABLE_TYPE)
            ❌ [ERR] The @provides directive on field 'Query.userByUsername' in schema 'Schema2' specifies an invalid field selection. (PROVIDES_INVALID_FIELDS)
               - The field 'id' does not exist on the type 'User'.
               - The field 'email' does not exist on the type 'User'.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema validation failed.
            """);
    }

    [Fact]
    public async Task Compose_IgnoredNonAccessibleFields()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-2/source-schema-a.graphqls");
        SetupSourceSchemaFromResources("valid-example-2/source-schema-b.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-2/source-schema-a.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-2/source-schema-b.graphqls"),
            "--archive",
            archiveFileName,
            "--include-satisfiability-paths");

        // assert
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Compose_Valid_ExcludeTag()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-exclude-by-tag/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-exclude-by-tag/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-exclude-by-tag/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-exclude-by-tag/source-schema-2.graphqls"),
            "--exclude-by-tag",
            "exclude-1",
            "--exclude-by-tag",
            "exclude-2",
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExcludeByTagCompositeSchema);
    }

    [Fact]
    public async Task Compose_Valid_Extensions()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-extensions/source-schema-1.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-extensions/source-schema-1.graphqls"),
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText
            .ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExtensionsCompositeSchema);
    }

    [Fact]
    public async Task Compose_Valid_Extensions_FromDirectory()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var workDir = Path.Combine(s_resourcesDir, "valid-extensions");

        // Set up the directory mock to return both the primary and extensions file.
        // Discovery must pick the primary schema, not the extensions sidecar.
        SetupDirectory(workDir,
            Path.Combine(workDir, "source-schema-1.graphqls"),
            Path.Combine(workDir, "source-schema-1-extensions.graphqls"));

        var schemaFile = Path.Combine(workDir, "source-schema-1.graphqls");
        var settingsFile = Path.Combine(workDir, "source-schema-1-settings.json");
        var extensionsFile = Path.Combine(workDir, "source-schema-1-extensions.graphqls");

        SetupFile(schemaFile, (await File.ReadAllTextAsync(
            schemaFile,
            TestContext.Current.CancellationToken)).TrimEnd());
        SetupFile(settingsFile, new MemoryStream(await File.ReadAllBytesAsync(
            settingsFile,
            TestContext.Current.CancellationToken)));
        SetupFile(extensionsFile, new MemoryStream(await File.ReadAllBytesAsync(
            extensionsFile,
            TestContext.Current.CancellationToken)));

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText
            .ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExtensionsCompositeSchema);
    }

    [Fact]
    public async Task Compose_Valid_Extensions_PointingToSidecar_ReturnsError()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var sidecarPath = Path.Combine(
            s_resourcesDir, "valid-extensions", "source-schema-1-extensions.graphqls");

        SetupFile(sidecarPath, new MemoryStream(await File.ReadAllBytesAsync(
            sidecarPath,
            TestContext.Current.CancellationToken)));

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            sidecarPath,
            "--archive",
            archiveFileName);

        // assert
        result.AssertError(
            $"Schema extensions file '{sidecarPath}' cannot be used as a source schema file. "
            + "Provide the base schema file instead.");
    }

    [Fact]
    public async Task Compose_MissingSettingsFile_ReturnsError()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        const string schemaFile = "/some/working/directory/missing-settings/schema.graphqls";
        SetupFile(schemaFile, "type Query { hello: String }");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFile,
            "--archive",
            archiveFileName);

        // assert
        result.AssertError(
            """
            Schema settings file '/some/working/directory/missing-settings/schema-settings.json' does not exist.
            """);
    }

    [Fact]
    public async Task Compose_InvalidJsonInSettingsFile_ReturnsError()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var schemaFile = Path.Combine(s_resourcesDir, "invalid-json/schema.graphqls");
        var settingsFile = Path.Combine(s_resourcesDir, "invalid-json/schema-settings.json");
        SetupFile(schemaFile, "type Query { hello: String }");
        SetupFile(settingsFile, "not valid json{{{");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFile,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.NotEmpty(result.StdErr);
    }

    [Fact]
    public async Task Compose_EnableGlobalObjectIdentification_ReturnsSuccess()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName,
            "--enable-global-object-identification");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_Should_RequireCompatibilityFlag_When_InterfaceObjectIsNotResolvable()
    {
        var strictArchive = CreateTempFile();
        var compatibilityArchive = CreateTempFile();
        SetupNonResolvableInterfaceObjectSchemas();

        var strictResult = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            "non-resolvable/a.graphqls",
            "--source-schema-file",
            "non-resolvable/b.graphqls",
            "--archive",
            strictArchive);
        var compatibilityResult = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            "non-resolvable/a.graphqls",
            "--source-schema-file",
            "non-resolvable/b.graphqls",
            "--archive",
            compatibilityArchive,
            "--allow-non-resolvable-interface-objects");

        Assert.Equal(1, strictResult.ExitCode);
        Assert.NotEmpty(strictResult.StdErr);
        Assert.Equal(0, compatibilityResult.ExitCode);

        using var archive = FusionArchive.Open(compatibilityArchive);
        using var settings = await archive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(settings);
        Assert.True(settings.RootElement
            .GetProperty("apolloFederationCompatibility")
            .GetProperty("allowNonResolvableInterfaceObjects")
            .GetBoolean());
    }

    [Fact]
    public async Task Compose_Should_PersistEveryUserFacingCompositionOption()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        var result = await ExecuteCommandAsync(
            CreateAllCompositionOptionArguments(archiveFileName));

        Assert.Equal(0, result.ExitCode);
        using var archive = FusionArchive.Open(archiveFileName);
        using var settings = await archive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(settings);
        settings.RootElement.ToString().MatchInlineSnapshot(
            """
            {
              "preprocessor": {
                "excludeByTag": [
                  "internal"
                ]
              },
              "merger": {
                "addFusionDefinitions": null,
                "cacheControlMergeBehavior": "Ignore",
                "enableGlobalObjectIdentification": true,
                "nodeResolution": "Gateway",
                "removeUnreferencedDefinitions": null,
                "tagMergeBehavior": "Include"
              },
              "satisfiability": {
                "includeSatisfiabilityPaths": true
              },
              "apolloFederationCompatibility": {
                "allowNonResolvableInterfaceObjects": true,
                "shareableFieldRuntimeTypeRouting": "CommonRuntimeTypes"
              }
            }
            """);
    }

    [Fact]
    public async Task ComposeWatch_Should_PersistSameCompositionSettingsAsOneShot()
    {
        var oneShotArchiveFileName = CreateTempFile();
        var watchArchiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        var oneShotResult = await ExecuteCommandAsync(
            CreateAllCompositionOptionArguments(oneShotArchiveFileName));
        Assert.Equal(0, oneShotResult.ExitCode);

        var watchArguments = CreateAllCompositionOptionArguments(watchArchiveFileName).ToList();
        watchArguments.Add("--watch");
        var watchCommand = StartInteractiveCommand([.. watchArguments]);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var watchTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);

        var watchSettingsJson = await WaitForCompositionSettingsAsync(
            watchArchiveFileName,
            cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();
        var watchResult = await watchTask;

        Assert.Equal(0, watchResult.ExitCode);
        using var oneShotArchive = FusionArchive.Open(oneShotArchiveFileName);
        using var oneShotSettings = await oneShotArchive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(oneShotSettings);
        Assert.Equal(oneShotSettings.RootElement.GetRawText(), watchSettingsJson);
    }

    [Fact]
    public async Task Compose_Should_PreserveOmittedCompatibilityFlag_AndApplyExplicitFalse()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");
        var schemaFiles = new[]
        {
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls")
        };

        var first = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName,
            "--allow-non-resolvable-interface-objects");
        Assert.Equal(0, first.ExitCode);
        SetupFile(archiveFileName, new MemoryStream(File.ReadAllBytes(archiveFileName)));

        var second = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName);
        Assert.Equal(0, second.ExitCode);
        using (var archive = FusionArchive.Open(archiveFileName))
        using (var settings = await archive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken))
        {
            Assert.NotNull(settings);
            Assert.True(settings.RootElement
                .GetProperty("apolloFederationCompatibility")
                .GetProperty("allowNonResolvableInterfaceObjects")
                .GetBoolean());
        }
        SetupFile(archiveFileName, new MemoryStream(File.ReadAllBytes(archiveFileName)));

        var third = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName,
            "--allow-non-resolvable-interface-objects",
            "false");
        Assert.Equal(0, third.ExitCode);
        using var finalArchive = FusionArchive.Open(archiveFileName);
        using var finalSettings = await finalArchive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(finalSettings);
        Assert.False(finalSettings.RootElement
            .GetProperty("apolloFederationCompatibility")
            .GetProperty("allowNonResolvableInterfaceObjects")
            .GetBoolean());
    }

    [Fact]
    public async Task Compose_Should_EmitExecutionMetadata_When_SourceSchemaNodeResolution()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName,
            "--enable-global-object-identification",
            "--node-resolution",
            "source-schema");

        Assert.Equal(0, result.ExitCode);
        using var archive = FusionArchive.Open(archiveFileName);
        var schema = await GetFusionSchemaAsync(archive);
        Assert.Contains("nodeResolution: SOURCE_SCHEMA", schema);
    }

    [Fact]
    public async Task Compose_Should_PersistShareableFieldRuntimeTypeRouting()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName,
            "--shareable-field-runtime-type-routing",
            "common-runtime-types");

        Assert.Equal(0, result.ExitCode);
        using var archive = FusionArchive.Open(archiveFileName);
        var settings = await archive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(settings);
        Assert.Contains(
            "\"shareableFieldRuntimeTypeRouting\": \"CommonRuntimeTypes\"",
            settings.RootElement.ToString());
        var schema = await GetFusionSchemaAsync(archive);
        Assert.Contains(
            "shareableFieldRuntimeTypeRouting: COMMON_RUNTIME_TYPES",
            schema);
    }

    [Fact]
    public async Task Compose_Should_ReturnError_When_SourceSchemaResolutionWithoutGlobalIdentification()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName,
            "--node-resolution",
            "source-schema");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Source-schema node resolution requires global object identification to be enabled.",
            result.StdErr);
    }

    [Fact]
    public async Task Compose_Should_PreserveArchiveSetting_When_NodeResolutionIsOmitted()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");
        var schemaFiles = new[]
        {
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls")
        };

        var first = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName,
            "--enable-global-object-identification",
            "--node-resolution",
            "source-schema");
        Assert.Equal(0, first.ExitCode);
        using (var firstArchive = FusionArchive.Open(archiveFileName))
        {
            var settings = await firstArchive.GetCompositionSettingsAsync(
                TestContext.Current.CancellationToken);
            Assert.NotNull(settings);
            Assert.Contains("\"nodeResolution\": \"SourceSchema\"", settings.RootElement.ToString());
        }
        SetupFile(archiveFileName, new MemoryStream(File.ReadAllBytes(archiveFileName)));

        var second = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName);

        Assert.Equal(0, second.ExitCode);
        using var archive = FusionArchive.Open(archiveFileName);
        var finalSettings = await archive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(finalSettings);
        Assert.Contains("\"nodeResolution\": \"SourceSchema\"", finalSettings.RootElement.ToString());
        var schema = await GetFusionSchemaAsync(archive);
        Assert.Contains("nodeResolution: SOURCE_SCHEMA", schema);
    }

    [Fact]
    public async Task Compose_Should_PreserveArchiveSetting_When_ShareableFieldRuntimeTypeRoutingIsOmitted()
    {
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");
        var schemaFiles = new[]
        {
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls")
        };

        var first = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName,
            "--shareable-field-runtime-type-routing",
            "common-runtime-types");
        Assert.Equal(0, first.ExitCode);
        using (var firstArchive = FusionArchive.Open(archiveFileName))
        {
            var settings = await firstArchive.GetCompositionSettingsAsync(
                TestContext.Current.CancellationToken);
            Assert.NotNull(settings);
            Assert.Contains(
                "\"shareableFieldRuntimeTypeRouting\": \"CommonRuntimeTypes\"",
                settings.RootElement.ToString());
        }
        SetupFile(archiveFileName, new MemoryStream(File.ReadAllBytes(archiveFileName)));

        var second = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            schemaFiles[0],
            "--source-schema-file",
            schemaFiles[1],
            "--archive",
            archiveFileName);

        Assert.Equal(0, second.ExitCode);
        using var archive = FusionArchive.Open(archiveFileName);
        var finalSettings = await archive.GetCompositionSettingsAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(finalSettings);
        Assert.Contains(
            "\"shareableFieldRuntimeTypeRouting\": \"CommonRuntimeTypes\"",
            finalSettings.RootElement.ToString());
        var schema = await GetFusionSchemaAsync(archive);
        Assert.Contains(
            "shareableFieldRuntimeTypeRouting: COMMON_RUNTIME_TYPES",
            schema);
    }

    [Fact]
    public async Task Compose_WithEnvironmentOption_ReturnsSuccess()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        SetupSourceSchemaFromResources("valid-example-1/source-schema-1.graphqls");
        SetupSourceSchemaFromResources("valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName,
            "--environment",
            "Production");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_AutoDiscoveryFromWorkingDirectory_ReturnsSuccess()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var workDir = Path.Combine(s_resourcesDir, "valid-example-1");
        SetupWorkingDirectoryWithSchemas(workDir,
            "source-schema-1.graphqls",
            "source-schema-2.graphqls");

        // act - no --source-schema-file specified, should auto-discover
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--working-directory",
            workDir,
            "--archive",
            archiveFileName);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    private static async Task<string> ReadSchemaAsync(GatewayConfiguration config)
    {
        await using var stream = await config.OpenReadSchemaAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static string[] CreateAllCompositionOptionArguments(string archiveFileName)
        =>
        [
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-2.graphqls"),
            "--archive",
            archiveFileName,
            "--cache-control-merge-behavior",
            "ignore",
            "--enable-global-object-identification",
            "--node-resolution",
            "gateway",
            "--tag-merge-behavior",
            "include",
            "--exclude-by-tag",
            "internal",
            "--include-satisfiability-paths",
            "--allow-non-resolvable-interface-objects",
            "--shareable-field-runtime-type-routing",
            "common-runtime-types"
        ];

    private static async Task<string> WaitForCompositionSettingsAsync(
        string archiveFileName,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(archiveFileName))
            {
                try
                {
                    using var archive = FusionArchive.Open(archiveFileName);
                    using var settings = await archive.GetCompositionSettingsAsync(cancellationToken);

                    if (settings is not null)
                    {
                        return settings.RootElement.GetRawText();
                    }
                }
                catch (IOException)
                {
                    // The watch composition is still committing the archive.
                }
                catch (InvalidDataException)
                {
                    // The watch composition is still committing the archive.
                }
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    private void SetupNonResolvableInterfaceObjectSchemas()
    {
        SetupFile("non-resolvable/a.graphqls", NonResolvableInterfaceObjectSchemaA);
        SetupFile(
            "non-resolvable/a-settings.json",
            """{ "name": "A", "transports": { "http": { "url": "http://a/graphql" } } }""");
        SetupFile("non-resolvable/b.graphqls", NonResolvableInterfaceObjectSchemaB);
        SetupFile(
            "non-resolvable/b-settings.json",
            """{ "name": "B", "transports": { "http": { "url": "http://b/graphql" } } }""");
    }

    private const string NonResolvableInterfaceObjectSchemaA =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])

        type Query {
          a: Node
        }

        interface Node @key(fields: "id") {
          id: ID!
        }

        type NodeImpl implements Node @key(fields: "id") {
          id: ID!
        }
        """;

    private const string NonResolvableInterfaceObjectSchemaB =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.6"
            import: ["@key", "@interfaceObject"])

        type Query {
          b: Node
        }

        type Node @key(fields: "id", resolvable: false) @interfaceObject {
          id: ID!
          field: String
        }
        """;

    private string CreateTempFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    /// <summary>
    /// Composes the given valid-example-1 source schemas into a fresh temp archive and
    /// returns its path. Used as the starting point for remove/replace compose runs.
    /// </summary>
    private async Task<string> BuildArchiveAsync(params string[] relativeSchemaPaths)
    {
        var archiveFileName = CreateTempFile();

        foreach (var relativeSchemaPath in relativeSchemaPaths)
        {
            SetupSourceSchemaFromResources(relativeSchemaPath);
        }

        var args = new List<string> { "fusion", "compose" };

        foreach (var relativeSchemaPath in relativeSchemaPaths)
        {
            args.Add("--source-schema-file");
            args.Add(Path.Combine(s_resourcesDir, relativeSchemaPath));
        }

        args.Add("--archive");
        args.Add(archiveFileName);

        var result = await ExecuteCommandAsync(args.ToArray());
        Assert.Equal(0, result.ExitCode);

        // Register the built archive on the mock file system so a follow-up compose run
        // opens it (and carries its source schemas forward) instead of recreating it empty.
        SetupFile(
            archiveFileName,
            new MemoryStream(await File.ReadAllBytesAsync(
                archiveFileName,
                TestContext.Current.CancellationToken)));

        return archiveFileName;
    }

    private void SetupReplacementSchema()
    {
        SetupFile("replacement/schema.graphqls", "type Query { schema3Field: Int! }");
        SetupFile(
            "replacement/schema-settings.json",
            "{ \"name\": \"Schema3\", \"transports\": { \"http\": { \"url\": \"http://localhost/graphql\" } } }");
    }

    /// <summary>
    /// Sets up a source schema file and its corresponding settings file
    /// on the mock file system, reading the content from the real __resources__ directory.
    /// </summary>
    private void SetupSourceSchemaFromResources(string relativePath)
    {
        var fullPath = Path.Combine(s_resourcesDir, relativePath);
        var settingsPath = Path.Combine(
            Path.GetDirectoryName(fullPath)!,
            Path.GetFileNameWithoutExtension(fullPath) + "-settings.json");
        var extensionsFilePath = Path.Combine(
            Path.GetDirectoryName(fullPath)!,
            Path.GetFileNameWithoutExtension(fullPath)
            + "-extensions"
            + Path.GetExtension(fullPath));

        SetupFile(fullPath, File.ReadAllText(fullPath));
        SetupFile(settingsPath, new MemoryStream(File.ReadAllBytes(settingsPath)));

        if (File.Exists(extensionsFilePath))
        {
            SetupFile(extensionsFilePath, new MemoryStream(File.ReadAllBytes(extensionsFilePath)));
        }
    }

    /// <summary>
    /// Sets up a working directory for auto-discovery with the given schema files.
    /// Configures the directory mock to return the schema files from GetFiles,
    /// and sets up each schema file and its settings on the mock file system.
    /// </summary>
    private void SetupWorkingDirectoryWithSchemas(
        string workDir,
        params string[] schemaFileNames)
    {
        var schemaFiles = schemaFileNames
            .Select(f => Path.Combine(workDir, f))
            .ToArray();

        SetupDirectory(workDir, schemaFiles);

        foreach (var schemaFileName in schemaFileNames)
        {
            var schemaFile = Path.Combine(workDir, schemaFileName);
            var settingsFile = Path.Combine(
                Path.GetDirectoryName(schemaFile)!,
                Path.GetFileNameWithoutExtension(schemaFile) + "-settings.json");

            SetupFile(schemaFile, File.ReadAllText(schemaFile));
            SetupFile(settingsFile, new MemoryStream(File.ReadAllBytes(settingsFile)));
        }
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (Directory.Exists(file))
                {
                    Directory.Delete(file, recursive: true);
                }
                else if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // ignore
            }
        }

        _tempFiles.Clear();
    }
}
