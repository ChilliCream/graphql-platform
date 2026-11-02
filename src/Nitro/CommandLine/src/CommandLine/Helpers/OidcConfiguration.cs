namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class OidcConfiguration
{
    public const string IdentityUrl = "https://identity.chillicream.com";
    public const string ClientId = BuildSecrets.NitroIdentityClientId;
    public const string Scopes = BuildSecrets.NitroIdentityScopes;
}
