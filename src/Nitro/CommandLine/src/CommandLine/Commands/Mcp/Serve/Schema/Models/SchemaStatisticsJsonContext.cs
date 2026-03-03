using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

[JsonSerializable(typeof(SchemaStatisticsResult))]
[JsonSerializable(typeof(GetSchemaError))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class SchemaStatisticsJsonContext : JsonSerializerContext;
