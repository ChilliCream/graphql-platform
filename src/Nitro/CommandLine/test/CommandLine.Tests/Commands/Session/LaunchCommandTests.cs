using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Session;

public sealed class LaunchCommandTests(NitroCommandFixture fixture) : SessionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "launch",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Launch Nitro in your default browser.

            Usage:
              nitro launch [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro launch
            """);
    }

    [Fact]
    public async Task NoSession_OpensDefaultNitroWebUrl()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        Assert.Equal(0, result.ExitCode);
        BrowserLauncherMock.Verify(x => x.Open(Constants.NitroWebUrl), Times.Once);
    }

    [Fact]
    public async Task DefaultApiUrl_OpensDefaultNitroWebUrl()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession();

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        Assert.Equal(0, result.ExitCode);
        BrowserLauncherMock.Verify(x => x.Open(Constants.NitroWebUrl), Times.Once);
    }

    [Fact]
    public async Task CustomApiUrl_OpensBaseUrlSlashUi()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            apiUrl: "api.custom.com",
            identityUrl: "https://id.custom.com");

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        Assert.Equal(0, result.ExitCode);
        BrowserLauncherMock.Verify(x => x.Open("https://api.custom.com/ui"), Times.Once);
    }

    [Fact]
    public async Task CustomApiUrlWithScheme_OpensBaseUrlSlashUi()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            apiUrl: "https://api.custom.com",
            identityUrl: "https://id.custom.com");

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        Assert.Equal(0, result.ExitCode);
        BrowserLauncherMock.Verify(x => x.Open("https://api.custom.com/ui"), Times.Once);
    }
}
