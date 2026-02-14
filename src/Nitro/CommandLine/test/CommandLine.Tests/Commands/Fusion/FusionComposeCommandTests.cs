using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionComposeCommandTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private static readonly string s_validExample1CompositeSchema =
        File.ReadAllText("__resources__/valid-example-1-result/composite-schema.graphqls");

    private static readonly string s_invalidExample1CompositeSchema =
        File.ReadAllText("__resources__/invalid-example-1-result/composite-schema.graphqls");

    private static readonly string s_validExcludeByTagCompositeSchema =
        File.ReadAllText("__resources__/valid-exclude-by-tag-result/composite-schema.graphqls");

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToStdOut()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var builder = GetCommandLineBuilder();

        string[] args =
        [
            "fusion",
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--archive",
            archiveFileName
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);

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
        var builder = GetCommandLineBuilder();
        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            "__resources__/valid-example-1",
            "--archive",
            archiveFileName
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);

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
        var builder = GetCommandLineBuilder();
        var archiveFileName = CreateTempFile();
        string[] args =
        [
            "fusion",
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--archive",
            archiveFileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
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
        var builder = GetCommandLineBuilder();
        var directory = Directory.GetCurrentDirectory();
        var fileName = $"../{Path.GetRandomFileName()}";
        var filePath = Path.Combine(directory, fileName);
        _tempFiles.Add(filePath);
        string[] args =
        [
            "fusion",
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--archive",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
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
        var builder = GetCommandLineBuilder();
        const string workingDirectory = "__resources__/valid-example-1";
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(workingDirectory, fileName);
        _tempFiles.Add(filePath);
        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            workingDirectory,
            "--archive",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
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
        var builder = GetCommandLineBuilder();
        const string workingDirectory = "__resources__/valid-example-1";
        var fileName = $"../{Path.GetRandomFileName()}";
        var filePath = Path.Combine(workingDirectory, fileName);
        _tempFiles.Add(filePath);
        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            workingDirectory,
            "--archive",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
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
        var builder = GetCommandLineBuilder();
        const string workingDirectory = "__resources__/valid-example-1";
        var filePath = CreateTempFile();

        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            workingDirectory,
            "--archive",
            filePath
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
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
        // arrange
        var builder = GetCommandLineBuilder();
        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            "non-existent",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(1, exitCode);
        Assert.Equal(
            "❌ Working directory 'non-existent' does not exist.",
            testConsole.Error.ToString()!.TrimEnd());
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInNewDirectory()
    {
        // arrange
        var builder = GetCommandLineBuilder();
        var archiveFileName = CreateTempFile();
        string[] args =
        [
            "fusion",
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--archive",
            archiveFileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(archiveFileName));
    }

    [Fact]
    public async Task Compose_FromNonExistentFiles()
    {
        // arrange
        var builder = GetCommandLineBuilder();
        string[] args =
        [
            "fusion",
            "compose",
            "--source-schema-file",
            "non-existent-1.graphqls",
            "--source-schema-file",
            "non-existent-2.graphqls"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(1, exitCode);
        Assert.Matches(
            "^❌ Source schema file '[^']*non-existent-1.graphqls' does not exist.$",
            testConsole.Out.ToString()!.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task Compose_InvalidExample1_FromWorkingDirectory_ToStdOutWithWarnings()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var builder = GetCommandLineBuilder();
        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            "__resources__/invalid-example-1",
            "--archive",
            archiveFileName,
            "--print"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);
        testConsole.Out.ToString()!.ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_invalidExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_InvalidExample2_FromWorkingDirectory_ToStdOutWithWarningsAndErrors()
    {
        // arrange
        var builder = GetCommandLineBuilder();
        string[] args =
        [
            "fusion",
            "compose",
            "--working-directory",
            "__resources__/invalid-example-2"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(1, exitCode);
        testConsole.Out.ToString()!.ReplaceLineEndings("\n").MatchSnapshot();
    }

    [Fact]
    public async Task Compose_IgnoredNonAccessibleFields()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var builder = GetCommandLineBuilder();

        string[] args =
        [
            "fusion",
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-2/source-schema-a.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-2/source-schema-b.graphqls",
            "--archive",
            archiveFileName,
            "--include-satisfiability-paths"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Compose_Valid_ExcludeTag()
    {
        // arrange
        var archiveFileName = CreateTempFile();
        var builder = GetCommandLineBuilder();

        string[] args =
        [
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
            archiveFileName
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);

        using var archive = FusionArchive.Open(archiveFileName);
        var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        var sourceText = await ReadSchemaAsync(config);
        sourceText.ReplaceLineEndings("\n").MatchInlineSnapshot(s_validExcludeByTagCompositeSchema);
    }

    private static CommandLineBuilder GetCommandLineBuilder()
    {
        var rootCommand = new Command("nitro");
        rootCommand.AddNitroCloudCommands();
        return new CommandLineBuilder(rootCommand).UseDefaults();
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
