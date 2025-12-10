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
    public const string IdentityUrl = "https://identity.chillicream.com";
    public const string ClientId = BuildSecrets.NitroIdentityClientId;
    public const string Scopes = BuildSecrets.NitroIdentityScopes;
}
