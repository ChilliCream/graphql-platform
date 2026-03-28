using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionSettingsSetCommandTests
{
    [Fact]
    public async Task SettingsSet_MissingArguments_ReturnsParseError()
    {
        // arrange
        var host = new CommandTestHost();

        // act
        var exitCode = await host.InvokeAsync("fusion", "settings", "set");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--archive' is required.
            Required argument missing for command: 'set'.
            Required argument missing for command: 'set'.
            """);
    }

    [Fact]
    public async Task SettingsSet_InvalidSettingName_ReturnsParseError()
    {
        // arrange
        var host = new CommandTestHost();

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "settings",
            "set",
            "not-a-setting",
            "value",
            "--archive",
            "/tmp/archive.far");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Argument 'not-a-setting' not recognized. Must be one of:
            	'cache-control-merge-behavior'
            	'exclude-by-tag'
            	'global-object-identification'
            	'tag-merge-behavior'
            """);
    }

    [Fact]
    public async Task SettingsSet_MissingArchiveOption_ReturnsParseError()
    {
        // arrange
        var host = new CommandTestHost();

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "settings",
            "set",
            "exclude-by-tag",
            "internal");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--archive' is required.
            """);
    }

    [Fact]
    public async Task SettingsSet_MissingArchiveFile_ReturnsError()
    {
        // arrange
        const string archivePath = "/tmp/nitro-fusion-settings-missing.far";
        var fileSystem = CreateFileSystem();
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "settings",
            "set",
            "exclude-by-tag",
            "internal",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ File '/tmp/nitro-fusion-settings-missing.far' does not exist.
            """);
        Assert.Empty(host.StdErr);
    }

    [Fact]
    public async Task SettingsSet_InvalidBooleanValue_ReturnsError()
    {
        // arrange
        var archivePath = CreateFilePath(".far");
        var fileSystem = CreateFileSystem((archivePath, "not-an-archive"));
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "settings",
            "set",
            "global-object-identification",
            "not-bool",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ Expected a boolean value for setting 'global-object-identification'.
            """);
        Assert.Empty(host.StdErr);
    }

    [Fact]
    public async Task SettingsSet_InvalidDirectiveMergeBehaviorValue_ReturnsError()
    {
        // arrange
        var archivePath = CreateFilePath(".far");
        var fileSystem = CreateFileSystem((archivePath, "not-an-archive"));
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "settings",
            "set",
            "cache-control-merge-behavior",
            "bad-value",
            "--archive",
            archivePath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ Expected one of the following values for setting
            'cache-control-merge-behavior': ignore, include, include-private
            """);
        Assert.Empty(host.StdErr);
    }

    private static CommandTestHost CreateHost(TestFileSystem? fileSystem = null)
    {
        var host = new CommandTestHost();
        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);

    private static TestFileSystem CreateFileSystem(params (string path, string content)[] seededFiles)
        => new(seededFiles.Select(x => new KeyValuePair<string, string>(x.path, x.content)).ToArray());
}
