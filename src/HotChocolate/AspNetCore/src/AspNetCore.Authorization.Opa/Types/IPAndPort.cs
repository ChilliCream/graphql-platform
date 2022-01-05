namespace HotChocolate.AspNetCore.Authorization;

public sealed class IPAndPort
{
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public static readonly IPAndPort Empty = new();
}
