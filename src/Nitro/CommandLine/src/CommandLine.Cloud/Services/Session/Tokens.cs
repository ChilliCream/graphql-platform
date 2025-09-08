namespace ChilliCream.Nitro.CommandLine.Cloud;

public sealed class Tokens(
    string accessToken,
    string idToken,
    string refreshToken,
    DateTimeOffset expiresAt)
{
    public string AccessToken { get; set; } = accessToken;

    public string IdToken { get; set; } = idToken;

    public string RefreshToken { get; set; } = refreshToken;

    public DateTimeOffset ExpiresAt { get; set; } = expiresAt;
}
