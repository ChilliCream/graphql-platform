using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record OpencodeHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("files")] Dictionary<string, OpencodeHooksSidecarEntry> Files)
{
    public const int CurrentVersion = 1;

    public static OpencodeHooksSidecarFile Empty => new(CurrentVersion, []);
}
