namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroDefaultsTests
{
    [Theory]
    [InlineData("https://api.chillicream.com", "https://nitro.chillicream.com/")]
    [InlineData("https://api.chillicream.cloud", "https://nitro.chillicream.cloud/")]
    [InlineData("http://nitro.example.test:8080/root?token=secret", "http://nitro.example.test:8080/ui")]
    public void CreatePortalUrl_Should_MapTheEffectiveApiUrl(
        string apiUrl,
        string expectedPortalUrl)
    {
        // act
        var portalUrl = NitroDefaults.CreatePortalUrl(new Uri(apiUrl));

        // assert
        Assert.Equal(expectedPortalUrl, portalUrl.ToString());
    }
}
