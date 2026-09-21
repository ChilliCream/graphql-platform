using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The installed notify arguments and prior foreign arguments, keyed by absolute
/// configuration path.
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
