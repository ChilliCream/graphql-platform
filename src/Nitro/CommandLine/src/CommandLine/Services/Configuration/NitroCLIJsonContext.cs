using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Commands.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Stages;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Services.Configuration;

[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(PersistedQueryStreamResult))]
[JsonSerializable(typeof(StageConfigurationParameter[]))]
internal partial class NitroCLIJsonContext : JsonSerializerContext;
