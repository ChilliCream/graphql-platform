namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroApiUrlTests
{
    [Theory]
    [InlineData("api.chillicream.com", "https://api.chillicream.com/")]
    [InlineData("https://api.chillicream.com", "https://api.chillicream.com/")]
    [InlineData("http://localhost:5000", "http://localhost:5000/")]
    [InlineData("api.chillicream.com/some/path?x=1#y", "https://api.chillicream.com/")]
    [InlineData("  api.chillicream.com  ", "https://api.chillicream.com/")]
    public void TryNormalize_Should_ProduceTheBaseUrl_When_TheValueIsAConfiguredApiUrl(
        string apiUrl,
        string expected)
    {
        // act
        var normalized = NitroApiUrl.TryNormalize(apiUrl, out var result);

        // assert
        Assert.True(normalized);
        Assert.Equal(expected, result!.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://")]
    public void TryNormalize_Should_Fail_When_TheValueIsNotAUrl(string? apiUrl)
    {
        // act
        var normalized = NitroApiUrl.TryNormalize(apiUrl, out var result);

        // assert
        Assert.False(normalized);
        Assert.Null(result);
    }

    [Fact]
    public void CreateGraphQLEndpoint_Should_UseTheGraphQLPath_When_TheApiUrlIsNormalized()
    {
        // arrange
        NitroApiUrl.TryNormalize("api.chillicream.com", out var apiUrl);

        // act
        var endpoint = NitroApiUrl.CreateGraphQLEndpoint(apiUrl!);

        // assert
        Assert.Equal("https://api.chillicream.com/graphql", endpoint.AbsoluteUri);
    }

    [Fact]
    public void CreateFusionConfigurationDownloadUrl_Should_MatchTheNitroClientRequest()
    {
        // arrange
        NitroApiUrl.TryNormalize("api.chillicream.com", out var apiUrl);

        // act
        var url = NitroApiUrl.CreateFusionConfigurationDownloadUrl(
            apiUrl!,
            "QXBpCmc1YzhkY2Uz",
            "dev",
            WellKnownVersions.LatestGatewayFormatVersion);

        // assert
        url.AbsoluteUri.MatchInlineSnapshot(
            "https://api.chillicream.com/api/v1/apis/QXBpCmc1YzhkY2Uz/fusion/configurations/latest/download?stage=dev&format=far&fusionVersion=2.0.0");
    }

    [Fact]
    public void CreateFusionConfigurationDownloadUrl_Should_EscapeValues_When_TheyCarrySeparators()
    {
        // arrange
        NitroApiUrl.TryNormalize("https://localhost:5001", out var apiUrl);

        // act
        var url = NitroApiUrl.CreateFusionConfigurationDownloadUrl(
            apiUrl!,
            "api/with?special&chars",
            "stage with spaces",
            WellKnownVersions.LatestGatewayFormatVersion);

        // assert
        url.AbsoluteUri.MatchInlineSnapshot(
            "https://localhost:5001/api/v1/apis/api%2Fwith%3Fspecial%26chars/fusion/configurations/latest/download?stage=stage%20with%20spaces&format=far&fusionVersion=2.0.0");
    }
}
