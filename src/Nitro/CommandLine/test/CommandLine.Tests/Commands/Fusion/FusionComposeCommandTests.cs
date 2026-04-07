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

    private static readonly string s_invalidExample1CompositeSchema =
        File.ReadAllText("__resources__/invalid-example-1-result/composite-schema.graphqls");

    private static readonly string s_validExcludeByTagCompositeSchema =
        File.ReadAllText("__resources__/valid-exclude-by-tag-result/composite-schema.graphqls");

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
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              -a, --archive, --configuration <archive>       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              --legacy-v1-archive <legacy-v1-archive>        The path to a Fusion v1 archive file. This option is only intended to be used during the migration from Fusion v1 to Fusion v2+.
              -e, --env, --environment <environment>         The name of the environment used for value substitution in the schema-settings.json files
              --enable-global-object-identification          Add the 'Query.node' field for global object identification
              --include-satisfiability-paths                 Include paths in satisfiability error messages
              --watch                                        Watch for file changes and recompose automatically
              -w, --working-directory <working-directory>    Set the working directory for the command
              --exclude-by-tag <exclude-by-tag>              One or more tags to exclude from the composition
              --cloud-url <cloud-url>                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information

            Example:
              nitro fusion compose \
                --source-schema-file ./products/schema.graphqls \
                --source-schema-file ./reviews/schema.graphqls \
                --archive ./gateway.far \
                --env "dev"
            """);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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
    public async Task Compose_WithLegacyArchive_FileDoesNotExist_ReturnsError()
    {
        // arrange
        var archiveFileName = CreateTempFile();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            Path.Combine(s_resourcesDir, "valid-example-1/source-schema-1.graphqls"),
            "--archive",
            archiveFileName,
            "--legacy-v1-archive",
            LegacyArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Legacy archive file '/some/working/directory/fusion-v1.fgp' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
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
            ❌ Source schema file '/path/to/non-existent-1.graphqls' does not exist.
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExcludeByTagCompositeSchema);
    }

    [Fact]
    public async Task Compose_MissingSettingsFile_ReturnsError()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var schemaFile = Path.Combine(s_resourcesDir, "missing-settings/schema.graphqls");
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
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Missing source schema settings file", result.StdErr);
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
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
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

    private string CreateTempFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _tempFiles.Add(tempFile);
        return tempFile;
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

        SetupFile(fullPath, File.ReadAllText(fullPath));
        SetupFile(settingsPath, new MemoryStream(File.ReadAllBytes(settingsPath)));
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
