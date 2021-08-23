using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            this JsonNode? jsonNode,
            [NotNullWhen(true)] out string? converted)
        {
            if (jsonNode is null)
            {
                converted = "null";
                return true;
            }

            return jsonNode.GetValue<JsonElement>().TryConvertToString(out converted);
        }

        public static bool TryConvertToNumber(
            this JsonNode? jsonNode,
            out double converted)
        {
            if (jsonNode is null)
            {
                converted = 0;
                return false;
            }

            return jsonNode.GetValue<JsonElement>().TryConvertToNumber(out converted);
        }

        public static bool TryConvertToComparable(
            this JsonNode? jsonNode,
            [NotNullWhen(true)] out IComparable? converted)
        {
            if (jsonNode is null)
            {
                converted = null;
                return false;
            }

            return jsonNode.GetValue<JsonElement>().TryConvertToComparable(out converted);
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
                    return false;
                case JsonValueKind.Number:
                    converted = jsonElement.GetDouble();
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
