using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

[JsonSerializable(typeof(BestPracticeGetResult))]
[JsonSerializable(typeof(BestPracticeSearchResponse))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class BestPracticeJsonContext : JsonSerializerContext;
