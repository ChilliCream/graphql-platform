using System.Text.Json;

namespace StrawberryShake.Transport.Http
{
    public static class JsonElementExtensions
    {
        public static JsonElement? GetPropertyOrNull(JsonElement jsonElement, string key)
        {
            if (jsonElement.TryGetProperty(key, out JsonElement property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                return property;
            }

            return null;
        }
    }
}
