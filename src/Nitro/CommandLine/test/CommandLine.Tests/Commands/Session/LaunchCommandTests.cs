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
        _browserLauncherMock
            .Setup(x => x.TryOpen(Constants.NitroWebUrl))
            .Returns(true);

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        result.AssertSuccess(
            """
            ✓ Nitro is launched!
            """);
        _browserLauncherMock.Verify(x => x.TryOpen(Constants.NitroWebUrl), Times.Once);
    }

    [Fact]
    public async Task DefaultApiUrl_OpensDefaultNitroWebUrl()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession();
        _browserLauncherMock
            .Setup(x => x.TryOpen(Constants.NitroWebUrl))
            .Returns(true);

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        result.AssertSuccess(
            """
            ✓ Nitro is launched!
            """);
        _browserLauncherMock.Verify(x => x.TryOpen(Constants.NitroWebUrl), Times.Once);
    }

    [Fact]
    public async Task CustomApiUrl_OpensBaseUrlSlashUi()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            apiUrl: "api.custom.com",
            identityUrl: "https://id.custom.com");
        _browserLauncherMock
            .Setup(x => x.TryOpen("https://api.custom.com/ui"))
            .Returns(true);

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        result.AssertSuccess(
            """
            ✓ Nitro is launched!
            """);
        _browserLauncherMock.Verify(x => x.TryOpen("https://api.custom.com/ui"), Times.Once);
    }

    [Fact]
    public async Task CustomApiUrlWithScheme_OpensBaseUrlSlashUi()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCustomSession(
            apiUrl: "https://api.custom.com",
            identityUrl: "https://id.custom.com");
        _browserLauncherMock
            .Setup(x => x.TryOpen("https://api.custom.com/ui"))
            .Returns(true);

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        result.AssertSuccess(
            """
            ✓ Nitro is launched!
            """);
        _browserLauncherMock.Verify(x => x.TryOpen("https://api.custom.com/ui"), Times.Once);
    }

    [Fact]
    public async Task BrowserCannotBeOpened_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        _browserLauncherMock
            .Setup(x => x.TryOpen(Constants.NitroWebUrl))
            .Returns(false);

        // act
        var result = await ExecuteCommandAsync("launch");

        // assert
        result.AssertError(
            """
            Could not open a browser at https://nitro.chillicream.com.
            """);
        _browserLauncherMock.Verify(x => x.TryOpen(Constants.NitroWebUrl), Times.Once);
    }
}
