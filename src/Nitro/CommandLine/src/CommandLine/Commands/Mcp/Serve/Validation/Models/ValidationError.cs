namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

internal sealed record ValidationError(
    string Type,
    string Message,
    int? Line = null,
    int? Column = null,
    int? Position = null,
    IReadOnlyList<string>? Details = null);
