namespace ChilliCream.Nitro.CommandLine.Cloud;

internal static class Constants
{
    // 5 Minutes
    public const int DefaultTimeoutSec = 60 * 5;

    public const string NitroWebUrl = "https://nitro.chillicream.com";

    public const string ApiUrl = "https://api.chillicream.com";
}

internal static class OidcConfiguration
{
    public const string IdentityUrl = "https://localhost:5004";
    public const string ClientId = "a2cd18b22ec34306a3dddeb78141d135";

    public const string Scopes = "openid profile offline_access";
}
