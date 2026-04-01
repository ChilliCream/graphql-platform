using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionComposeCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>, IDisposable
{
    private readonly List<string> _tempFiles = [];

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
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToStdOut()
    {
        // arrange
        var archiveFileName = CreateTempFile();

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

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

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                "__resources__/valid-example-1",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

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

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

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
        var directory = Directory.GetCurrentDirectory();
        var fileName = $"../{Path.GetRandomFileName()}";
        var filePath = Path.Combine(directory, fileName);
        _tempFiles.Add(filePath);

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                fileName)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(filePath));

        using var archive = FusionArchive.Open(fileName);
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileInWorkingDirectory()
    {
        // arrange
        const string workingDirectory = "__resources__/valid-example-1";
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(workingDirectory, fileName);
        _tempFiles.Add(filePath);

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                workingDirectory,
                "--archive",
                fileName)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(filePath));

        using var archive = FusionArchive.Open(filePath);
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileRelativeToWorkingDirectory()
    {
        // arrange
        const string workingDirectory = "__resources__/valid-example-1";
        var fileName = $"../{Path.GetRandomFileName()}";
        var filePath = Path.Combine(workingDirectory, fileName);
        _tempFiles.Add(filePath);

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                workingDirectory,
                "--archive",
                fileName)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(filePath));

        using var archive = FusionArchive.Open(filePath);
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileAtFullyQualifiedPath()
    {
        // arrange
        const string workingDirectory = "__resources__/valid-example-1";
        var filePath = CreateTempFile();

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                workingDirectory,
                "--archive",
                filePath)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(filePath));

        using var archive = FusionArchive.Open(filePath);
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInNonExistentWorkingDirectory()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                "non-existent",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls")
            .ExecuteAsync();

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

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_FromNonExistentFiles()
    {
        // arrange & act
        const string nonExistentFile = "non-existent-1.graphqls";
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                nonExistentFile)
            .ExecuteAsync();

        // assert
        var nonExistentFilePath = Path.GetFullPath(nonExistentFile);

        // The console wraps long paths across lines, so normalize before replacing
        var stderr = result.StdErr.Replace("\n", "").Replace("\r", "");
        stderr = stderr.Replace(nonExistentFilePath, "/path/to/" + nonExistentFile);

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

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                "__resources__/invalid-example-1",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

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
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                "__resources__/invalid-example-2")
            .ExecuteAsync();

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

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-2/source-schema-a.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-2/source-schema-b.graphqls",
                "--archive",
                archiveFileName,
                "--include-satisfiability-paths")
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Compose_Valid_ExcludeTag()
    {
        // arrange
        var archiveFileName = CreateTempFile();

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-exclude-by-tag/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-exclude-by-tag/source-schema-2.graphqls",
                "--exclude-by-tag",
                "exclude-1",
                "--exclude-by-tag",
                "exclude-2",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

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
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var schemaFile = Path.Combine(tempDir, "schema.graphqls");
        await File.WriteAllTextAsync(schemaFile, "type Query { hello: String }");
        _tempFiles.Add(tempDir);

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                schemaFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Missing source schema settings file", result.StdErr);
    }

    [Fact]
    public async Task Compose_InvalidJsonInSettingsFile_ReturnsError()
    {
        // arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var schemaFile = Path.Combine(tempDir, "schema.graphqls");
        var settingsFile = Path.Combine(tempDir, "schema-settings.json");
        await File.WriteAllTextAsync(schemaFile, "type Query { hello: String }");
        await File.WriteAllTextAsync(settingsFile, "not valid json{{{");
        _tempFiles.Add(tempDir);

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                schemaFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.NotEmpty(result.StdErr);
    }

    [Fact]
    public async Task Compose_EnableGlobalObjectIdentification_ReturnsSuccess()
    {
        // arrange
        var archiveFileName = CreateTempFile();

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                archiveFileName,
                "--enable-global-object-identification")
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_WithEnvironmentOption_ReturnsSuccess()
    {
        // arrange
        var archiveFileName = CreateTempFile();

        // act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                archiveFileName,
                "--environment",
                "Production")
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_AutoDiscoveryFromWorkingDirectory_ReturnsSuccess()
    {
        // arrange
        var archiveFileName = CreateTempFile();

        // act - no --source-schema-file specified, should auto-discover
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--working-directory",
                "__resources__/valid-example-1",
                "--archive",
                archiveFileName)
            .ExecuteAsync();

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
