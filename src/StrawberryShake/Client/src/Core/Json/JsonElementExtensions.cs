using System.Text.Json;

namespace StrawberryShake.Json;

public static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement obj, string key)
        => obj.TryGetProperty(key, out JsonElement property) &&
            property.ValueKind is not JsonValueKind.Null
            ? property
            : null;

    public static JsonElement? GetPropertyOrNull(this JsonElement? obj, string key)
        => obj.HasValue ? GetPropertyOrNull(obj.Value, key) : null;

    public static bool ContainsFragment(this JsonElement? obj, string key)
        => obj.HasValue &&
            obj.Value.TryGetProperty(key, out JsonElement property) &&
            property.ValueKind is JsonValueKind.String;
}
