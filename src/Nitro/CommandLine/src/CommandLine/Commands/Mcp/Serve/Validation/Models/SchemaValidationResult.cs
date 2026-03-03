namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

internal sealed record SchemaValidationResult(
    bool Valid,
    IReadOnlyList<SchemaChangeEntry> Changes,
    IReadOnlyList<ValidationError> Errors);
