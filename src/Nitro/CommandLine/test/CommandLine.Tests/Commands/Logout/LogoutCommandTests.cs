using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Logout;

public sealed class LogoutCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Logout_Should_OutputHelpText()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "logout",
                "--help")
            .ExecuteAsync();

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

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task Logout_Should_Succeed_When_LogoutCompletes(InteractionMode mode)
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments("logout");

        builder.SessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess();

        builder.SessionServiceMock.Verify(
            x => x.LogoutAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_Should_Succeed_When_LogoutCompletes_NonInteractive()
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("logout");

        builder.SessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Logging out
            └── ✓ Logged out. See you soon 👋
            """);

        builder.SessionServiceMock.Verify(
            x => x.LogoutAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_Should_ReturnError_When_LogoutThrowsExitException()
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("logout");

        builder.SessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExitException("Session deletion failed."));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Session deletion failed.
            """);
        Assert.Equal(1, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LogoutAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_Should_ReturnError_When_LogoutThrowsExitException_NonInteractive()
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("logout");

        builder.SessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExitException("Session deletion failed."));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Logging out
            └── ✕ Failed to log out.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Session deletion failed.
            """);
        Assert.Equal(1, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LogoutAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_Should_ReturnError_When_LogoutThrowsUnexpectedException()
    {
        // arrange
        var builder = new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("logout");

        builder.SessionServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Unexpected error
            """);
        Assert.Equal(1, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LogoutAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
