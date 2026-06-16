using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionSourceSchemaRemoveCommandTests(NitroCommandFixture fixture)
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
            "remove",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Remove a source schema from a Fusion archive and recompose the remaining source schemas.

            Usage:
              nitro fusion source-schema remove <SOURCE_SCHEMA_NAME> [options]

            Arguments:
              <SOURCE_SCHEMA_NAME>  The name of the source schema to remove

            Options:
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -e, --env, --environment <environment>               The name of the environment used for value substitution in the schema-settings.json files
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                                  The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information

            Example:
              nitro fusion source-schema remove reviews \
                --archive ./gateway.far
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
            "remove",
            "Schema2");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--archive' is required.
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
            "remove",
            "Schema2",
            "--archive",
            "fusion.far");

        // assert
        result.AssertError("Archive file '/some/working/directory/fusion.far' does not exist.");
    }

    [Fact]
    public async Task SourceSchemaDoesNotExist_ReturnsError()
    {
        // arrange
        var archiveFileName = await ComposeArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "remove",
            "DoesNotExist",
            "--archive",
            archiveFileName);

        // assert
        result.AssertError(
            $"Source schema 'DoesNotExist' does not exist in the Fusion archive '{archiveFileName}'.");
    }

    [Fact]
    public async Task Remove_ExistingSourceSchema_RecomposesArchive()
    {
        // arrange
        var archiveFileName = await ComposeArchiveAsync(
            "valid-example-1/source-schema-1.graphqls",
            "valid-example-1/source-schema-2.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "remove",
            "Schema2",
            "--archive",
            archiveFileName);

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
    public async Task Remove_LastSourceSchema_ReturnsError()
    {
        // arrange
        var archiveFileName = await ComposeArchiveAsync(
            "valid-example-1/source-schema-1.graphqls");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "remove",
            "Schema1",
            "--archive",
            archiveFileName);

        // assert
        result.AssertError(
            "Source schema 'Schema1' cannot be removed because it is the only source schema in the Fusion archive.");
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
                if (File.Exists(file))
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
