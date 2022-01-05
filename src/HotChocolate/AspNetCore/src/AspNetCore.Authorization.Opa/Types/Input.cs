namespace HotChocolate.AspNetCore.Authorization;

public sealed class Input
{
    public GraphQl GraphQL { get; set; } = new GraphQl();
    public OriginalRequest Request { get; set; } = new();
    public IPAndPort Source { get; set; } = IPAndPort.Empty;
    public IPAndPort Destination { get; set; } = IPAndPort.Empty;
    public static readonly Input Empty = new();
}
