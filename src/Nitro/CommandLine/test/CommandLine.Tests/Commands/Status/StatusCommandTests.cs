using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Status;

public sealed class StatusCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "status",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Display the current session status.

            Usage:
              nitro status [options]

            Options:
              -?, -h, --help  Show help and usage information
            """);
    }

    [Fact]
    public async Task Status_Should_ReturnError_When_NotLoggedIn()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("status")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Not logged in. Run 'nitro login' first.
            """);
    }

    [Fact]
    public async Task Status_Should_DisplayEmail_When_DefaultApiUrl()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddSession()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("status")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com
            """);
    }

    [Fact]
    public async Task Status_Should_DisplayEmailAndWorkspace_When_WorkspaceSet()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("status")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com (Workspace from session workspace)
            """);
    }

    [Fact]
    public async Task Status_Should_DisplayApiUrl_When_CustomApiUrl()
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("status");

        builder.SessionServiceMock
            .SetupGet(x => x.Session)
            .Returns(new Session(
                "session-1",
                "subject-1",
                "tenant-1",
                "https://id.custom.com",
                "api.custom.com",
                "user@chillicream.com",
                tokens: null,
                workspace: null));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com on api.custom.com
            """);
    }

    [Fact]
    public async Task Status_Should_DisplayAll_When_CustomApiUrlAndWorkspace()
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("status");

        builder.SessionServiceMock
            .SetupGet(x => x.Session)
            .Returns(new Session(
                "session-1",
                "subject-1",
                "tenant-1",
                "https://id.custom.com",
                "api.custom.com",
                "user@chillicream.com",
                tokens: null,
                workspace: new Workspace("ws-1", "my-workspace")));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Logged in as user@chillicream.com on api.custom.com (my-workspace workspace)
            """);
    }
}
