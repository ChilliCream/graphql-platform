using System.CommandLine;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionDownloadCommandTests : FusionCommandTestBase
{
    private readonly NitroCommandFixture _fixture;

    public FusionDownloadCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download the most recent router configuration.

            Usage:
              nitro fusion download [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --version <version>           The version of the archive to download [default: 2.0.0]
              --output-file <output-file>   The file path to write the output to [env: NITRO_OUTPUT_FILE]
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>           The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro fusion download \
                --api-id "<api-id>" \
                --stage "dev" \
                --output-file ./gateway.far
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    #region Option Validation

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
            "download");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--stage' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidVersion_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--version",
            "invalid");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--version' received an invalid value: invalid
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    [Theory]
    [InlineData("2.0.0", "far", null, false, InteractionMode.Interactive)]
    [InlineData("1.0.0", "fgp", null, false, InteractionMode.Interactive)]
    [InlineData("2.0.0", "far", null, false, InteractionMode.NonInteractive)]
    [InlineData("1.0.0", "fgp", null, false, InteractionMode.NonInteractive)]
    [InlineData("2.0.0", "far", null, false, InteractionMode.JsonOutput)]
    [InlineData("1.0.0", "fgp", null, false, InteractionMode.JsonOutput)]
    [InlineData("2.0.0", "far", "graph.far", false, InteractionMode.NonInteractive)]
    [InlineData("1.0.0", "fgp", "custom.fgp", false, InteractionMode.NonInteractive)]
    [InlineData("2.0.0", "far", "graph.far", true, InteractionMode.NonInteractive)]
    [InlineData("1.0.0", "fgp", "custom.fgp", true, InteractionMode.NonInteractive)]
    public async Task Download_Should_WriteExpectedFile_When_OutputPathIsProvidedOrOmitted(
        string version,
        string format,
        string? outputFile,
        bool fromEnvironment,
        InteractionMode mode)
    {
        var directory = Directory.CreateTempSubdirectory();
        var expectedFilename = outputFile ?? "gateway." + format;
        var expectedPath = Path.Combine(directory.FullName, expectedFilename);
        var archiveBytes = await File.ReadAllBytesAsync(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway." + format),
            TestContext.Current.CancellationToken);
        SetupFusionConfigurationDownload(version, format);

        // Forward file operations to disk, with a per-test working directory.
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GetCurrentDirectory()).Returns(directory.FullName);
        fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(File.Exists);
        fileSystem.Setup(x => x.DeleteFile(It.IsAny<string>())).Callback<string>(File.Delete);
        fileSystem.Setup(x => x.CreateFile(It.IsAny<string>())).Returns((string path) => File.Create(path));
        var environment = new Mock<IEnvironmentVariableProvider>();
        var arguments = new List<string>
        {
            "fusion", "download", "--api-id", ApiId, "--stage", Stage,
            "--api-key", "default-api-key"
        };

        if (version == "1.0.0")
        {
            arguments.AddRange(["--version", version]);
        }

        if (fromEnvironment)
        {
            environment.Setup(x => x.GetEnvironmentVariable("NITRO_OUTPUT_FILE"))
                .Returns(expectedPath);
        }
        else if (outputFile is not null)
        {
            arguments.AddRange(["--output-file", outputFile]);
        }

        if (mode is InteractionMode.JsonOutput)
        {
            arguments.AddRange(["--output", "json"]);
        }

        await using var stdout = new StringWriter();
        await using var stderr = new StringWriter();
        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(stdout);
        // Avoid line wrapping based on the machine-specific temporary directory length.
        outConsole.Profile.Width = 1024;
        outConsole.Profile.Capabilities.Interactive = mode is not InteractionMode.NonInteractive;
        var errConsole = new TestConsole();
        errConsole.Profile.Out = new AnsiConsoleOutput(stderr);
        var services = new ServiceCollection();
        services.AddSingleton(fileSystem.Object);
        services.AddSingleton(environment.Object);
        services.AddSingleton(_sessionServiceMock.Object);
        services.AddSingleton(FusionConfigurationClientMock.Object);
        services.AddSingleton<NitroClientContext>();
        services.AddSingleton<INitroConsole>(new NitroConsole(
            outConsole, errConsole, environment.Object, new SnapshotActivitySinkFactory()));
        services.AddNitroServices();
        await using var provider = services.BuildServiceProvider();

        try
        {
            await File.WriteAllTextAsync(expectedPath, "previous archive", TestContext.Current.CancellationToken);

            var exitCode = await _fixture.RootCommand.ExecuteAsync(
                arguments,
                provider,
                new InvocationConfiguration { Output = stdout, Error = stderr },
                TestContext.Current.CancellationToken);

            Snapshot.Create($"{expectedFilename}-{mode}")
                .Add(exitCode, "Exit code")
                .Add(stdout.ToString().TrimEnd().Replace(directory.FullName, "<working-directory>"), "Standard output")
                .Add(stderr.ToString().TrimEnd(), "Standard error")
                .Add(Directory.GetFiles(directory.FullName).Select(Path.GetFileName).Order(), "Files")
                .MatchMarkdownSnapshot();
            Assert.Equal(archiveBytes, await File.ReadAllBytesAsync(expectedPath, TestContext.Current.CancellationToken));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task DownloadFarFile_ReturnsSuccess()
    {
        // arrange
        SetupFusionConfigurationDownload();
        var outputStream = SetupCreateFile(ArchiveFile);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Downloaded Fusion configuration to '/some/working/directory/fusion.far'.
            """);
        Assert.True(outputStream.ToArray().Length > 0);
    }

    [Fact]
    public async Task DownloadFgpFile_ReturnsSuccess()
    {
        // arrange
        SetupFusionConfigurationDownload("1.0.0", ArchiveFormats.Fgp);
        var outputStream = SetupCreateFile("gateway.fgp");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            "gateway.fgp",
            "--version",
            "1.0.0");

        // assert
        result.AssertSuccess(
            """
            Downloaded Fusion configuration to '/some/working/directory/gateway.fgp'.
            """);
        Assert.True(outputStream.ToArray().Length > 0);
    }

    [Fact]
    public async Task DownloadFgpFile_VersionIsNot1_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            "gateway.fgp",
            "--version",
            "2.0.0");

        // assert
        result.AssertError(
            """
            Specify '--version 1.0.0' if you want to download a '.fgp' legacy Fusion archive.
            """);
    }

    [Fact]
    public async Task DownloadFarFile_VersionIs1_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            "graph.far",
            "--version",
            "1.0.0");

        // assert
        result.AssertError(
            """
            Specify the '.fgp' extension through the '--output-file' option, if you want to download a legacy Fusion archive.
            """);
    }

    [Fact]
    public async Task FusionConfigurationNotFound_ReturnsError()
    {
        // arrange
        SetupMissingFusionConfigurationDownload();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            The API with the given ID does not exist or there is no Fusion configuration that supports version '2.0.0'.
            """);
    }
}
