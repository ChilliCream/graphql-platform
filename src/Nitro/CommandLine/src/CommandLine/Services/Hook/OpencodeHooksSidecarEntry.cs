using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record OpencodeHooksSidecarEntry(
    [property: JsonPropertyName("launchCommand")] string LaunchCommand,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt);
