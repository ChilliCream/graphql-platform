using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

[JsonSerializable(typeof(ManagementError))]
[JsonSerializable(typeof(CreateApiResult))]
[JsonSerializable(typeof(CreateApiKeyResult))]
[JsonSerializable(typeof(CreateClientResult))]
[JsonSerializable(typeof(ListApisResult))]
[JsonSerializable(typeof(ListApiKeysResult))]
[JsonSerializable(typeof(ListClientsResult))]
[JsonSerializable(typeof(UpdateApiSettingsResult))]
internal partial class ManagementJsonContext : JsonSerializerContext;
