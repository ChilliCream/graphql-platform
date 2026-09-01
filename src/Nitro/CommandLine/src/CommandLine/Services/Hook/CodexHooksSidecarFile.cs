using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The sidecar file recording only the <c>config.toml</c> <c>notify</c>
/// value Nitro replaced, keyed by the absolute config path. <c>hooks.json</c>
/// ownership is deterministic: uninstall removes managed-event groups whose
/// commands are Nitro Codex hook invocations, so it needs no provenance file.
/// </summary>
internal sealed record CodexHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("notifyFiles")]
        Dictionary<string, CodexNotifySidecarEntry> NotifyFiles)
{
    public const int CurrentVersion = 2;

    public static CodexHooksSidecarFile Empty => new(CurrentVersion, []);

    public CodexNotifySidecarEntry? NotifyEntryFor(string configTomlPath)
        => NotifyFiles.GetValueOrDefault(configTomlPath);
}
