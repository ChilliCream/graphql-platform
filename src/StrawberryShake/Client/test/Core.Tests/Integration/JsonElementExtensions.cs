using System.Text.Json;

namespace StrawberryShake.Integration
{
    public static class JsonElementExtensions
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement jsonElement, string key)
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
