namespace ChilliCream.Nitro.CommandLine.Tests;

internal static class CommandExecutionResultExtensions
{
    public static void AssertError(this CommandExecutionResult result, string stderr)
    {
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(stderr);
    }

    public static void AssertSuccess(this CommandExecutionResult result, string stdout)
    {
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(stdout);
    }
}
