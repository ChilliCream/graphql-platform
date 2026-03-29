namespace ChilliCream.Nitro.CommandLine.Tests;

internal static class CommandExecutionResultExtensions
{
    public static void AssertError(this CommandResult result, string stderr)
    {
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(stderr);
        Assert.Equal(1, result.ExitCode);
    }

    public static void AssertSuccess(this CommandResult result, string stdout)
    {
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(stdout);
        Assert.Equal(0, result.ExitCode);
    }

    public static void AssertHelpOutput(this CommandResult result, string stdout)
    {
        Assert.Empty(result.StdErr);
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(stdout);
        Assert.Equal(0, result.ExitCode);
    }
}
