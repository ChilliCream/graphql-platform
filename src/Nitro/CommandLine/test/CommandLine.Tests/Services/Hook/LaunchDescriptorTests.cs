using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests the PowerShell command lines <see cref="LaunchDescriptor"/> builds for harnesses that run
/// Windows hooks through <c>powershell.exe -Command</c>.
/// </summary>
public sealed class LaunchDescriptorTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-launch-descriptor-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public void BuildPowerShellCommandLine_Should_CallTheQuotedExecutable_When_PathsContainSpacesAndApostrophes()
    {
        // arrange
        var descriptor = new LaunchDescriptor(
            @"C:\Program Files\dotnet\dotnet.exe",
            [@"C:\Users\o'brien\nitro tools\nitro.dll"]);

        // act
        var command = descriptor.BuildPowerShellCommandLine(["agent", "hook", "venti", "stop"]);

        // assert
        command.MatchInlineSnapshot(
            """
            & 'C:\Program Files\dotnet\dotnet.exe' 'C:\Users\o''brien\nitro tools\nitro.dll' agent hook venti stop
            """);
    }

    [Fact]
    public void PowerShellQuote_Should_DoubleEverySingleQuoteCharacter_When_TheTokenContainsTypographicQuotes()
    {
        // arrange
        // act
        var quoted = LaunchDescriptor.PowerShellQuote("C:\\it\u2019s \u2018a\u2019 dir");

        // assert
        Assert.Equal("'C:\\it\u2019\u2019s \u2018\u2018a\u2019\u2019 dir'", quoted);
    }

    [Fact]
    public void BuildPowerShellCommandLine_Should_LeaveTheCommandUnquoted_When_NoTokenNeedsQuoting()
    {
        // arrange
        var descriptor = new LaunchDescriptor(@"C:\tools\nitro.exe", []);

        // act
        var command = descriptor.BuildPowerShellCommandLine(["agent", "hook", "venti", "session-start"]);

        // assert
        command.MatchInlineSnapshot(@"C:\tools\nitro.exe agent hook venti session-start");
    }

    [Fact]
    public async Task BuildPowerShellCommandLine_Should_PassEveryArgumentIntact_When_PowerShellRunsTheCommand()
    {
        // arrange
        var powerShell = FindPowerShell();
        Assert.SkipWhen(powerShell is null, "PowerShell is not installed.");
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = Path.Combine(_tempRoot.FullName, "it's a dir");
        Directory.CreateDirectory(directory);
        var executable = await WriteArgumentEchoAsync(directory, cancellationToken);
        var assembly = Path.Combine(directory, "nitro tool's.dll");
        var command = new LaunchDescriptor(executable, [assembly])
            .BuildPowerShellCommandLine(["agent", "hook", "venti", "stop"]);

        // act
        var start = new ProcessStartInfo(powerShell!) { ArgumentList = { "-NoProfile", "-Command", command } };
        start.RedirectStandardOutput = true;
        using var process = Process.Start(start)!;
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        // assert
        Assert.Equal(
            (0, $"{assembly}\nagent\nhook\nventi\nstop"),
            (process.ExitCode, output.ReplaceLineEndings("\n").TrimEnd()));
    }

    private static async Task<string> WriteArgumentEchoAsync(string directory, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            var batch = Path.Combine(directory, "echo args.cmd");
            await File.WriteAllTextAsync(
                batch,
                "@echo off\r\n:next\r\nif \"%~1\"==\"\" goto :eof\r\necho %~1\r\nshift\r\ngoto next\r\n",
                cancellationToken);
            return batch;
        }

        var script = Path.Combine(directory, "echo args.sh");
        await File.WriteAllTextAsync(script, "#!/bin/sh\nprintf '%s\\n' \"$@\"\n", cancellationToken);
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return script;
    }

    private static string? FindPowerShell()
    {
        string[] names = OperatingSystem.IsWindows() ? ["pwsh.exe", "powershell.exe"] : ["pwsh"];

        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(directory => names.Select(name => Path.Combine(directory, name)))
            .FirstOrDefault(File.Exists);
    }
}
