using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionSourceSchemaReplaceCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture), IDisposable
{
    private readonly List<string> _tempFiles = [];

    private static readonly string s_resourcesDir =
        Path.GetFullPath("__resources__");

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Replace a source schema in a Fusion archive with an updated source schema and recompose.

            Usage:
              nitro fusion source-schema replace <OLD_SOURCE_SCHEMA_NAME> [options]

            Arguments:
              <OLD_SOURCE_SCHEMA_NAME>  The name of the source schema to replace

            Options:
              -a, --archive, --configuration <archive> (REQUIRED)       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file> (REQUIRED)  The path to a source schema file (.graphqls) or directory containing a source schema file
              -e, --env, --environment <environment>                    The name of the environment used for value substitution in the schema-settings.json files
              -w, --working-directory <working-directory>               Set the working directory for the command
              --cloud-url <cloud-url>                                   The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                                       The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                           The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                            Show help and usage information

            Example:
              nitro fusion source-schema replace reviews \
                --archive ./gateway.far \
                --source-schema-file ./reviews/schema.graphqls
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingArchiveOption_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "Schema2",
            "--source-schema-file",
            "replacement/schema.graphqls");

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
    public async Task MissingSourceSchemaFileOption_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "Schema2",
            "--archive",
            "fusion.far");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--source-schema-file' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ArchiveDoesNotExist_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "Schema2",
            "--archive",
            "fusion.far",
            "--source-schema-file",
            "replacement/schema.graphqls");

        // assert
        result.AssertError("Archive file '/some/working/directory/fusion.far' does not exist.");
    }

    [Fact]
    public async Task OldSourceSchemaDoesNotExist_ReturnsError()
    {
        // arrange
        var archiveFileName = await ComposeArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        SetupReplacementSchema();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "DoesNotExist",
            "--archive",
            archiveFileName,
            "--source-schema-file",
            "replacement/schema.graphqls");

        // assert
        result.AssertError(
            $"Source schema 'DoesNotExist' does not exist in the Fusion archive '{archiveFileName}'.");
    }

    [Fact]
    public async Task Replace_Rename_RecomposesArchive()
    {
        // arrange
        var archiveFileName = await ComposeArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        SetupReplacementSchema();
        var output = SetupCreateFile(archiveFileName);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "Schema2",
            "--archive",
            archiveFileName,
            "--source-schema-file",
            "replacement/schema.graphqls");

        // assert
        Assert.Equal(0, result.ExitCode);

        using var resultArchive = FusionArchive.Open(new MemoryStream(output.ToArray()));
        var names = await resultArchive.GetSourceSchemaNamesAsync(TestContext.Current.CancellationToken);
        var schema = await GetFusionSchemaAsync(resultArchive);
        Assert.Equal(["Schema1", "Schema3"], names);
        Assert.Contains("schema3Field", schema);
        Assert.DoesNotContain("schema2Field", schema);
    }

    [Fact]
    public async Task Replace_RelativeArchive_ResolvesAgainstWorkingDirectory()
    {
        // arrange
        var archiveBytes = await ComposeArchiveBytesAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        var workingDirectory = CreateTempDirectory();
        var archivePath = Path.Combine(workingDirectory, "gateway.far");
        SetupFile(archivePath, new MemoryStream(archiveBytes));
        SetupReplacementSchema();
        var output = SetupCreateFile(archivePath);

        // act
        // The relative --archive must resolve under --working-directory, while the
        // --source-schema-file is passed as an absolute path so it is found regardless.
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "Schema2",
            "--archive",
            "gateway.far",
            "--source-schema-file",
            "/some/working/directory/replacement/schema.graphqls",
            "--working-directory",
            workingDirectory);

        // assert
        Assert.Equal(0, result.ExitCode);

        using var resultArchive = FusionArchive.Open(new MemoryStream(output.ToArray()));
        var names = await resultArchive.GetSourceSchemaNamesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Schema1", "Schema3"], names);
    }

    [Fact]
    public async Task Replace_CompositionFails_DoesNotWriteArchive()
    {
        // arrange
        var archiveFileName = await ComposeArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");
        SetupInvalidReplacementSchema();
        var output = SetupCreateFile(archiveFileName);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "replace",
            "Schema2",
            "--archive",
            archiveFileName,
            "--source-schema-file",
            "replacement/schema.graphqls");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(output.ToArray());
    }

    private void SetupReplacementSchema()
    {
        SetupFile("replacement/schema.graphqls", "type Query { schema3Field: Int! }");
        SetupFile(
            "replacement/schema-settings.json",
            "{ \"name\": \"Schema3\", \"transports\": { \"http\": { \"url\": \"http://localhost/graphql\" } } }");
    }

    private void SetupInvalidReplacementSchema()
    {
        SetupFile(
            "replacement/schema.graphqls",
            "type Query { field(arg: String @require(field: \"non-existent\")): String }");
        SetupFile(
            "replacement/schema-settings.json",
            "{ \"name\": \"Schema3\", \"transports\": { \"http\": { \"url\": \"http://localhost/graphql\" } } }");
    }

    private async Task<string> ComposeArchiveAsync(params string[] relativeSchemaPaths)
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

        var compose = await ExecuteCommandAsync(args.ToArray());
        Assert.Equal(0, compose.ExitCode);

        SetupFile(
            archiveFileName,
            new MemoryStream(await File.ReadAllBytesAsync(
                archiveFileName,
                TestContext.Current.CancellationToken)));

        return archiveFileName;
    }

    private async Task<byte[]> ComposeArchiveBytesAsync(params string[] relativeSchemaPaths)
    {
        var archiveFileName = await ComposeArchiveAsync(relativeSchemaPaths);

        return await File.ReadAllBytesAsync(
            archiveFileName,
            TestContext.Current.CancellationToken);
    }

    private string CreateTempDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        _tempFiles.Add(tempDirectory);
        return tempDirectory;
    }

    private void SetupSourceSchemaFromResources(string relativePath)
    {
        var fullPath = Path.Combine(s_resourcesDir, relativePath);
        var settingsPath = Path.Combine(
            Path.GetDirectoryName(fullPath)!,
            Path.GetFileNameWithoutExtension(fullPath) + "-settings.json");

        SetupFile(fullPath, File.ReadAllText(fullPath));
        SetupFile(settingsPath, new MemoryStream(File.ReadAllBytes(settingsPath)));
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
