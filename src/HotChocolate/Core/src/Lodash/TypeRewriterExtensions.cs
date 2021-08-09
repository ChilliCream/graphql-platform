using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace HotChocolate.Lodash
{
    public static class TypeRewriterExtensions
    {
        public static bool TryConvertToString(
            this JsonElement? jsonElement,
            [NotNullWhen(true)] out string? converted)
        {
            if (jsonElement.HasValue)
            {
                return TryConvertToString(jsonElement.Value, out converted);
            }

            converted = null;
            return false;
        }

        public static bool TryConvertToString(
            this JsonElement jsonElement,
            [NotNullWhen(true)] out string? converted)
        {
            converted = null;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Undefined:
                    converted = "undefined";
                    return true;
                case JsonValueKind.Object:
                    return false;
                case JsonValueKind.Array:
                    return false;
                case JsonValueKind.String:
                    converted = jsonElement.GetString() ?? string.Empty;
                    return true;
                case JsonValueKind.Number:
                    converted = jsonElement.GetDouble().ToString(CultureInfo.InvariantCulture);
                    return true;
                case JsonValueKind.True:
                    converted = "true";
                    return true;
                case JsonValueKind.False:
                    converted = "false";
                    return true;
                case JsonValueKind.Null:
                    converted = "null";
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
