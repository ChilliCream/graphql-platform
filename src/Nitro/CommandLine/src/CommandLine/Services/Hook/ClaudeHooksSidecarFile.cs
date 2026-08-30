using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The sidecar file recording exactly which Claude Code <c>settings.json</c>
/// hook entries this CLI installed, keyed by the absolute path of the
/// settings file (one machine can have both a user-scope and a
/// project-scope installation). Lives under the platform application-data
/// directory alongside the instance id, never inside a harness config
/// directory, so it survives a foreign edit or reinstall of the config file
/// itself.
/// </summary>
internal sealed record ClaudeHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("files")]
        Dictionary<string, Dictionary<string, ClaudeHooksSidecarEntry>> Files)
{
    public const int CurrentVersion = 1;

    public static ClaudeHooksSidecarFile Empty => new(CurrentVersion, []);

    public IReadOnlyDictionary<string, ClaudeHooksSidecarEntry> EntriesFor(string settingsFilePath)
        => Files.TryGetValue(settingsFilePath, out var events)
            ? events
            : new Dictionary<string, ClaudeHooksSidecarEntry>();
}
