namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Session;

public sealed class LogoutCommandTests(NitroCommandFixture fixture) : SessionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "logout",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Log out and remove session information.

            Usage:
              nitro logout [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro logout
            """);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupLogout();

        // act
        var result = await ExecuteCommandAsync("logout");

        // assert
        result.AssertSuccess(
            """
            Logging out
            └── ✓ Logged out. See you soon 👋
            """);
    }

    [Fact]
    public async Task LogoutThrows_ReturnsError()
    {
        // arrange
        SetupLogoutThrows();

        // act
        var result = await ExecuteCommandAsync("logout");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Logging out
            └── ✕ Failed to log out.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }
}
