namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

internal sealed record SchemaChangeEntry(string Severity, string ChangeType, string? Coordinate, string Description);
