using System.Text.Json;

namespace StrawberryShake.Json;

internal static class JsonExtensionParser
{
    public static IReadOnlyDictionary<string, object?>? ParseExtensions(JsonElement result)
    {
        if (result is { ValueKind: JsonValueKind.Object, })
        {
            var extensions = JsonSerializationHelper.ReadValue(result);
            return (IReadOnlyDictionary<string, object?>?)extensions;
        }

        return null;
    }
}
