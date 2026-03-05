namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class ArgumentEntry
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? Description { get; init; }
    public string? DefaultValue { get; init; }
    public bool IsDeprecated { get; init; }
    public string? DeprecationReason { get; init; }
}
