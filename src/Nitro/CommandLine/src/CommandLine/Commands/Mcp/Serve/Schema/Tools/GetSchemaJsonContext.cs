using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

[JsonSerializable(typeof(GetSchemaResult))]
[JsonSerializable(typeof(GetSchemaError))]
internal partial class GetSchemaJsonContext : JsonSerializerContext;
