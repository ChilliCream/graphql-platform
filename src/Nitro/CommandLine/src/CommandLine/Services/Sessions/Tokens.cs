#if FUSION_ASPIRE
namespace HotChocolate.Fusion.Aspire;

internal sealed class Tokens(
#else
namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

public sealed class Tokens(
#endif
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
