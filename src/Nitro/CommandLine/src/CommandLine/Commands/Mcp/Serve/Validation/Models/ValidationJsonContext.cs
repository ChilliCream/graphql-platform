using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

[JsonSerializable(typeof(SchemaValidationResult))]
[JsonSerializable(typeof(ClientValidationResult))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
internal partial class ValidationJsonContext : JsonSerializerContext;
