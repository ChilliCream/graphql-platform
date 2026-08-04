namespace ChilliCream.Nitro.Client;

public interface INitroClientAuthorization;

public sealed record NitroClientApiKeyAuthorization(string ApiKey)
    : INitroClientAuthorization;

public sealed record NitroClientAccessTokenAuthorization(string AccessToken)
    : INitroClientAuthorization;
