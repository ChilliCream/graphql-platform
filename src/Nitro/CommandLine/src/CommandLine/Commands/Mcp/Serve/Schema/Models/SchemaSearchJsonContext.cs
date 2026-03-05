using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(GetMembersResult))]
[JsonSerializable(typeof(GetSchemaError))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class SchemaSearchJsonContext : JsonSerializerContext;
