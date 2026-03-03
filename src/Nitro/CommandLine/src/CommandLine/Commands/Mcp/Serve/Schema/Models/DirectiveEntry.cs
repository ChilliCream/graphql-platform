namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class DirectiveEntry
{
    public required string Name { get; init; }
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }
}
