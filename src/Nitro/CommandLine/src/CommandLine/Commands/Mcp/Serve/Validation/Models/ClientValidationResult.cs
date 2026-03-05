namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

internal sealed record ClientValidationResult(bool Valid, IReadOnlyList<ClientValidationError> Errors);
