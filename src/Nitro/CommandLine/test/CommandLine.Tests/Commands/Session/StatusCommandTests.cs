namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Session;

public sealed class StatusCommandTests(NitroCommandFixture fixture) : SessionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "status",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Display the current session status.

            Usage:
              nitro status [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro status
            """);
    }

    [Fact]
    public async Task NotLoggedIn_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("status");

        // assert
        result.AssertError(
            """
            Not logged in. Run 'nitro login' first.
            """);
    }

    [Fact]
    public async Task DefaultApiUrl_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession();

        // act
        var result = await ExecuteCommandAsync("status");

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com
            """);
    }

    [Fact]
    public async Task WithWorkspace_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            workspaceId: "ws-1",
            workspaceName: "Workspace from session");

        // act
        var result = await ExecuteCommandAsync("status");

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com (Workspace from session workspace)
            """);
    }

    [Fact]
    public async Task CustomApiUrl_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            apiUrl: "api.custom.com",
            identityUrl: "https://id.custom.com");

        // act
        var result = await ExecuteCommandAsync("status");

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com on api.custom.com
            """);
    }

    [Fact]
    public async Task CustomApiUrl_WithWorkspace_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            apiUrl: "api.custom.com",
            identityUrl: "https://id.custom.com",
            workspaceId: "ws-1",
            workspaceName: "my-workspace");

        // act
        var result = await ExecuteCommandAsync("status");

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com on api.custom.com (my-workspace workspace)
            """);
    }
}
