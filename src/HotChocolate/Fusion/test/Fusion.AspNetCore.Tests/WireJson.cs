using System.Text.Json;

namespace HotChocolate.Fusion;

internal static class WireJson
{
    private static readonly JsonSerializerOptions s_indented = new() { WriteIndented = true };

    public static string Indent(string json)
    {
        using var document = JsonDocument.Parse(json);

        return JsonSerializer.Serialize(document.RootElement, s_indented);
    }
}
