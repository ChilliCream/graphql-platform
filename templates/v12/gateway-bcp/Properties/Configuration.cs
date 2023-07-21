public sealed class Configuration
{
    public string ApiId { get; } = Environment.GetEnvironmentVariable("BCP-API-ID") ??
        throw new InvalidOperationException("BCP-API-ID missing.");
    public string ApiKey { get; } = Environment.GetEnvironmentVariable("BCP-API-KEY") ??
        throw new InvalidOperationException("BCP-API-KEY missing.");
    public string Stage { get; } = Environment.GetEnvironmentVariable("BCP-STAGE") ??
        throw new InvalidOperationException("BCP-STAGE missing.");
}
