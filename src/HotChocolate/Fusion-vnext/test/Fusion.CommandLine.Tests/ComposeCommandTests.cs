using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using RootCommand = HotChocolate.Fusion.Commands.RootCommand;

namespace HotChocolate.Fusion;

public sealed class ComposeCommandTests
{
    private static readonly string s_validExample1CompositeSchema =
        File.ReadAllText("__resources__/valid-example-1-result/composite-schema.graphqls");

    private static readonly string s_invalidExample1CompositeSchema =
        File.ReadAllText("__resources__/invalid-example-1-result/composite-schema.graphqls");

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToStdOut()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        string[] args =
        [
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);
        testConsole.Out.ToString()!.ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToStdOut()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        string[] args =
        [
            "compose",
            "--working-directory",
            "__resources__/valid-example-1"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(0, exitCode);
        testConsole.Out.ToString()!.ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInCurrentDirectory()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        var directory = Directory.GetCurrentDirectory();
        const string fileName = "valid-example-1-composite-schema.graphqls";
        var filePath = Path.Combine(directory, fileName);
        string[] args =
        [
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--composite-schema-file",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(filePath));
        (await File.ReadAllTextAsync(filePath)).ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);

        // cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileRelativeToCurrentDirectory()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        var directory = Directory.GetCurrentDirectory();
        const string fileName = "../valid-example-1-composite-schema.graphqls";
        var filePath = Path.Combine(directory, fileName);
        string[] args =
        [
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--composite-schema-file",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(filePath));
        (await File.ReadAllTextAsync(filePath)).ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);

        // cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileInWorkingDirectory()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        const string workingDirectory = "__resources__/valid-example-1";
        const string fileName = "valid-example-1-composite-schema.graphqls";
        var filePath = Path.Combine(workingDirectory, fileName);
        string[] args =
        [
            "compose",
            "--working-directory",
            workingDirectory,
            "--composite-schema-file",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(filePath));
        (await File.ReadAllTextAsync(filePath)).ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);

        // cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileRelativeToWorkingDirectory()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        const string workingDirectory = "__resources__/valid-example-1";
        const string fileName = "../valid-example-1-composite-schema.graphqls";
        var filePath = Path.Combine(workingDirectory, fileName);
        string[] args =
        [
            "compose",
            "--working-directory",
            workingDirectory,
            "--composite-schema-file",
            fileName
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(filePath));
        (await File.ReadAllTextAsync(filePath)).ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);

        // cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromWorkingDirectory_ToFileAtFullyQualifiedPath()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        const string workingDirectory = "__resources__/valid-example-1";
        var filePath = Path.GetTempFileName();
        string[] args =
        [
            "compose",
            "--working-directory",
            workingDirectory,
            "--composite-schema-file",
            filePath
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(filePath));
        (await File.ReadAllTextAsync(filePath)).ReplaceLineEndings("\n")
            .MatchInlineSnapshot(s_validExample1CompositeSchema);

        // cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task Compose_ValidExample1_FromSpecified_ToFileInNonExistentWorkingDirectory()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        const string fileName = "valid-example-1-composite-schema.graphqls";
        string[] args =
        [
            "compose",
            "--working-directory",
            "non-existent",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--composite-schema-file",
            fileName
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
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        const string directory = "new";
        const string fileName = "valid-example-1-composite-schema.graphqls";
        var filePath = Path.Combine(directory, fileName);
        string[] args =
        [
            "compose",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-1.graphqls",
            "--source-schema-file",
            "__resources__/valid-example-1/source-schema-2.graphqls",
            "--composite-schema-file",
            filePath
        ];

        // act
        var exitCode = await builder.Build().InvokeAsync(args);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(filePath));

        // cleanup
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task Compose_FromNonExistentFiles()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        string[] args =
        [
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
            testConsole.Error.ToString()!.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task Compose_InvalidExample1_FromWorkingDirectory_ToStdOutWithWarnings()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        string[] args =
        [
            "compose",
            "--working-directory",
            "__resources__/invalid-example-1"
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
    public async Task Compose_InvalidExample1_FromWorkingDirectory_ToStdOutWithWarningsAndErrors()
    {
        // arrange
        var builder = new CommandLineBuilder(new RootCommand()).UseDefaults();
        string[] args =
        [
            "compose",
            "--working-directory",
            "__resources__/invalid-example-2"
        ];
        var testConsole = new TestConsole();

        // act
        var exitCode = await builder.Build().InvokeAsync(args, testConsole);

        // assert
        Assert.Equal(1, exitCode);
        testConsole.Error.ToString()!.ReplaceLineEndings("\n").MatchSnapshot();
    }
}
