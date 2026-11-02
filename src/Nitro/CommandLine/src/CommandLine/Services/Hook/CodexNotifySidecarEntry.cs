using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The installed notify arguments and the foreign arguments they replaced.
/// <see cref="PriorForeign"/> is null when no foreign command was recorded.
/// </summary>
internal sealed record CodexNotifySidecarEntry(
    [property: JsonPropertyName("ourArgv")] IReadOnlyList<string> OurArgv,
    [property: JsonPropertyName("priorForeign")] IReadOnlyList<string>? PriorForeign,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt);
