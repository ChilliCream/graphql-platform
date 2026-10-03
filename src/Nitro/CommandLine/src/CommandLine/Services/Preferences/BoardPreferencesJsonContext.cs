using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Preferences;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(BoardPreferencesFile))]
internal sealed partial class BoardPreferencesJsonContext : JsonSerializerContext;
