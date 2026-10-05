using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Preferences;

/// <summary>
/// The persisted board preferences. A missing or unrecognized
/// <paramref name="BoardOrientation"/> means the default orientation.
/// </summary>
internal sealed record BoardPreferencesFile(
    [property: JsonPropertyName("boardOrientation")] string? BoardOrientation);
