namespace HotChocolate.AspNetCore.Authorization;

public sealed class Input
{
    public Policy Policy { get; set; } = new();
    public OriginalRequest Request { get; set; } = new();
    public IPAndPort Source { get; set; } = IPAndPort.Empty;
    public IPAndPort Destination { get; set; } = IPAndPort.Empty;
    public object? Extensions { get; set; }
    public static readonly Input Empty = new();
}
