namespace ChilliCream.Nitro.CommandLine.Cloud;

internal static class Constants
{
    // 5 Minutes
    public const int DefaultTimeoutSec = 60 * 5;

    public const string NitroWebUrl = "https://nitro.chillicream.com";
}

internal static class OidcConfiguration
{
    public const string IdentityUrl = "https://identity.chillicream.com";
    public const string ClientId = "<<NITRO_IDENTITY_CLIENT_ID>>"; // TODO: Replace with actual client id during build
    public const string Scopes = "<<NITRO_IDENTITY_SCOPES>>"; // TODO: Replace with actual client id during build
}
