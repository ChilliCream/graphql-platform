namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

internal sealed record ClientValidationError(
    string Type,
    string Message,
    string? Hash = null,
    IReadOnlyList<ErrorLocation>? Locations = null);
