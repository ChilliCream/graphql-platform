using System.Net;
using ChilliCream.Nitro.CommandLine.Helpers;
using Duende.IdentityModel.OidcClient.Browser;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Helpers;

public sealed class SystemBrowserTests
{
    [Theory]
    [InlineData("https://identity.example.com")]
    [InlineData("https://identity.example.com/")]
    public async Task InvokeAsync_Should_RedirectToClosePageOfIdentityServer_When_CallbackIsReceived(
        string identityUrl)
    {
        // arrange
        var browserLauncher = new Mock<IBrowserLauncher>();
        browserLauncher.Setup(x => x.TryOpen(It.IsAny<string>())).Returns(true);
        var browser = new SystemBrowser(identityUrl, browserLauncher.Object, (_, _) => { });
        var login = browser.InvokeAsync(
            new BrowserOptions("https://identity.example.com/connect/authorize", browser.RedirectUri),
            TestContext.Current.CancellationToken);
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        // act
        using var response = await client.GetAsync(
            $"{browser.RedirectUri}?code=abc&state=xyz",
            TestContext.Current.CancellationToken);
        var result = await login;

        // assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(
            "https://identity.example.com/Account/Close",
            response.Headers.Location?.ToString());
        Assert.Equal("?code=abc&state=xyz", result.Response);
    }
}
