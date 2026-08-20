using ChilliCream.Nitro.CommandLine.Tests.Commands;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal static class CommandExecutionResultExtensions
{
    public static void AssertError(this CommandResult result, string stderr)
    {
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(stderr);
        Assert.Equal(1, result.ExitCode);
    }

    public static void AssertSuccess(this CommandResult result, string? stdout = null)
    {
        Assert.Empty(result.StdErr);
        if (stdout is not null)
        {
            result.StdOut.MatchInlineSnapshot(stdout);
        }
        Assert.Equal(0, result.ExitCode);
    }

    public static void AssertHelpOutput(this CommandResult result, string stdout)
    {
        Assert.Empty(result.StdErr);
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(stdout);
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Asserts the guidance a bare group command with subcommands but no
    /// action prints: the "Required command was not provided." parse error
    /// on stderr, and the group's own help (with the executable name
    /// normalized the same way <see cref="AssertHelpOutput"/> does) on
    /// stdout, exiting 1.
    /// </summary>
    public static void AssertBareGroupGuidance(this CommandResult result, string stdout)
    {
        result.StdErr.MatchInlineSnapshot("Required command was not provided.");
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(stdout);
        Assert.Equal(1, result.ExitCode);
    }
}
