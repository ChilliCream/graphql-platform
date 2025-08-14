using System.Text.Json.Serialization;
using ChilliCream.Nitro.CLI.Commands.Client;
using ChilliCream.Nitro.CLI.Commands.Stages;

namespace ChilliCream.Nitro.CLI;

[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(PersistedQueryStreamResult))]
[JsonSerializable(typeof(StageConfigurationParameter[]))]
internal partial class NitroCLIJsonContext : JsonSerializerContext
{
}
