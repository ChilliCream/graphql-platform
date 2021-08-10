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

        public static bool TryConvertToComparable(
            this JsonElement jsonElement,
            [NotNullWhen(true)] out IComparable? converted)
        {
            converted = null;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Undefined:
                    return false;
                case JsonValueKind.Object:
                    return false;
                case JsonValueKind.Array:
                    return false;
                case JsonValueKind.String:
                    //TODO : DateTime?
                    converted = jsonElement.GetString() ?? string.Empty;
                    return true;
                case JsonValueKind.Number:
                    converted = jsonElement.GetDouble().ToString(CultureInfo.InvariantCulture);
                    return true;
                case JsonValueKind.True:
                    converted = true;
                    return true;
                case JsonValueKind.False:
                    converted = false;
                    return true;
                case JsonValueKind.Null:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool TryConvertToNumber(
            this JsonElement jsonElement,
            out double number)
        {
            number = 0;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Undefined:
                    return false;
                case JsonValueKind.Object:
                    return false;
                case JsonValueKind.Array:
                    return false;
                case JsonValueKind.String:
                    return false;
                case JsonValueKind.Number:
                    number = jsonElement.GetDouble();
                    return true;
                case JsonValueKind.True:
                    return false;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
