using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Stages;

namespace ChilliCream.Nitro.CommandLine.Cloud;

[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(PersistedQueryStreamResult))]
[JsonSerializable(typeof(StageConfigurationParameter[]))]
internal partial class NitroCLIJsonContext : JsonSerializerContext;
