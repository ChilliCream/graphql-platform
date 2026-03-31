using ChilliCream.Nitro.CommandLine.Helpers;
using HotChocolate.Fusion.Packaging;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionSettingsSetCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>, IDisposable
{
    private readonly List<string> _tempFiles = [];

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set a Fusion composition setting in a Fusion archive.

            Usage:
              nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> [options]

            Arguments:
              <cache-control-merge-behavior|exclude-by-tag|global-object-identification|tag-merge-behavior>  The name of the setting to change
              <SETTING_VALUE>                                                                                The value to set

            Options:
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -e, --env, --environment <environment>               The name of the environment used for value substitution in the schema-settings.json files
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "cache-control-merge-behavior",
                "ignore")
            .ExecuteAsync();

        // assert
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(
            """
            Description:
              Set a Fusion composition setting in a Fusion archive.

            Usage:
              nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> [options]

            Arguments:
              <cache-control-merge-behavior|exclude-by-tag|global-object-identification|tag-merge-behavior>  The name of the setting to change
              <SETTING_VALUE>                                                                                The value to set

            Options:
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -e, --env, --environment <environment>               The name of the environment used for value substitution in the schema-settings.json files
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information
            """);
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
    public async Task FileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("/tmp/nonexistent.far"))
            .Returns(false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(fileSystem.Object)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "cache-control-merge-behavior",
                "ignore",
                "--archive",
                "/tmp/nonexistent.far")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            File '/tmp/nonexistent.far' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);

        fileSystem.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidCacheControlMergeBehavior_ReturnsError(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "cache-control-merge-behavior",
                "invalid-value",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Expected one of the following values for setting 'cache-control-merge-behavior':
            ignore, include, include-private
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task InvalidTagMergeBehavior_ReturnsError(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "tag-merge-behavior",
                "invalid-value",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Expected one of the following values for setting 'tag-merge-behavior': ignore,
            include, include-private
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task InvalidGlobalObjectIdentification_ReturnsError(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "global-object-identification",
                "not-a-bool",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Expected a boolean value for setting 'global-object-identification'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task CacheControlMergeBehavior_Ignore_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "cache-control-merge-behavior",
                "ignore",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task TagMergeBehavior_Include_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "tag-merge-behavior",
                "include",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task GlobalObjectIdentification_True_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "global-object-identification",
                "true",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task ExcludeByTag_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "exclude-by-tag",
                "tag1,tag2",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
    }

    [Fact]
    public async Task CacheControlMergeBehavior_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "cache-control-merge-behavior",
                "ignore",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "setting": "cache-control-merge-behavior",
              "value": "ignore"
            }
            """);
    }

    [Fact]
    public async Task InvalidSettingName_ReturnsError()
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "nonexistent-setting",
                "some-value",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("nonexistent-setting", result.StdErr);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task ExcludeByTag_MultipleTags_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "exclude-by-tag",
                "tag1, tag2, tag3",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
    }

    [Fact]
    public async Task ExcludeByTag_MultipleTags_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var archiveFile = CreateArchiveWithSourceSchemas();

        // act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "settings",
                "set",
                "exclude-by-tag",
                "tag1, tag2, tag3",
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "setting": "exclude-by-tag",
              "value": "tag1, tag2, tag3"
            }
            """);
    }

    private string CreateArchiveWithSourceSchemas()
    {
        var archiveFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".far");
        _tempFiles.Add(archiveFile);

        // Create an archive by running the compose command
        var result = new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "compose",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-1.graphqls",
                "--source-schema-file",
                "__resources__/valid-example-1/source-schema-2.graphqls",
                "--archive",
                archiveFile)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(archiveFile));

        return archiveFile;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // ignore
                }
            }
        }

        _tempFiles.Clear();
    }
}
