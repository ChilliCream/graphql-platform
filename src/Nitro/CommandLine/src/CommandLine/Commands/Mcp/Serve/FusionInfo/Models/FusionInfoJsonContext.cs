using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;

[JsonSerializable(typeof(FusionInfoResult))]
[JsonSerializable(typeof(FusionInfoError))]
internal partial class FusionInfoJsonContext : JsonSerializerContext;
