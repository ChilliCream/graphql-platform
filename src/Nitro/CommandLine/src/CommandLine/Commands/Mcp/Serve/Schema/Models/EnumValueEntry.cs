namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class EnumValueEntry
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsDeprecated { get; init; }
    public string? DeprecationReason { get; init; }
}
