namespace ChilliCream.Nitro.Client;

public readonly record struct NitroAuthHeader(string Name, string Value)
{
    public static NitroAuthHeader Bearer(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new NitroAuthHeader("Authorization", $"Bearer {token}");
    }

    public static NitroAuthHeader ApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return new NitroAuthHeader("CCC-api-key", apiKey);
    }
}
