using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The <c>config.toml</c> <c>notify</c> key's recorded installation:
/// provenance for the foreign-wrapping contract. <see cref="PriorForeign"/>
/// is null when no foreign <c>notify</c> was configured at install time (so
/// uninstall removes the key entirely rather than writing back an empty
/// array); non-null preserves exactly what was there so uninstall can
/// restore it verbatim (semantic restoration, same non-goal as
/// <see cref="ClaudeHooksSidecarFile"/>: not a byte-accurate restore of the
/// whole config file).
/// </summary>
internal sealed record CodexNotifySidecarEntry(
    [property: JsonPropertyName("ourArgv")] IReadOnlyList<string> OurArgv,
    [property: JsonPropertyName("priorForeign")] IReadOnlyList<string>? PriorForeign,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt);
