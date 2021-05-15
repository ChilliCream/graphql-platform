namespace HotChocolate.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    public static class JsonDocumentConverter
    {
        public static object Convert(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            object value = null;

            if (obj is JsonElement jsonElement)
            {
                Action<object> setValue = delegate (object v)
                {
                    value = v;
                };
                ConvertValue(jsonElement, setValue, new HashSet<object>());
            }

            return value;
        }

        private static void ConvertValue(JsonElement element, Action<object> setValue, ISet<object> processed)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.False:
                case JsonValueKind.True:
                    setValue(GetValue(element));
                    break;
                case JsonValueKind.Array:
                    ConvertArray(element, setValue, processed);
                    break;
                case JsonValueKind.Object:
                    ConvertObject(element, setValue, processed);
                    break;
                default:
                    setValue(null);
                    break;
            }
        }

        private static void ConvertArray(JsonElement element, Action<object> setValue, ISet<object> processed)
        {
            var valueList = new List<object>();
            setValue(valueList);
            Action<object> setValue2 = delegate (object item)
            {
                valueList.Add(item);
            };
            foreach (JsonElement item in element.EnumerateArray())
            {
                ConvertValue(item, setValue2, processed);
            }
        }

        private static void ConvertObject(JsonElement element, Action<object> setValue, ISet<object> processed)
        {
            if (!processed.Add(element))
            {
                return;
            }

            var dict = new Dictionary<string, object>();
            setValue(dict);
            foreach (JsonProperty jsonProperty in element.EnumerateObject())
            {
                Action<object> setValue2 = delegate (object v)
                {
                    dict[jsonProperty.Name] = v;
                };
                ConvertValue(jsonProperty.Value, setValue2, processed);
            }
        }

        private static object GetValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    if (element.TryGetDateTimeOffset(out DateTimeOffset dateTimeOffset))
                    {
                        return dateTimeOffset;
                    }

                    if (element.TryGetGuid(out Guid guid))
                    {
                        return guid;
                    }

                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt16(out var int16))
                    {
                        return int16;
                    }

                    if (element.TryGetInt32(out var int32))
                    {
                        return int32;
                    }

                    if (element.TryGetInt64(out var int64))
                    {
                        return int64;
                    }

                    if (element.TryGetDecimal(out var decimalValue))
                    {
                        return decimalValue;
                    }

                    if (element.TryGetSingle(out var floatValue) && !(float.IsInfinity(floatValue) || float.IsNegativeInfinity(floatValue)))
                    {
                        return floatValue;
                    }

                    if (element.TryGetDouble(out var doubleValue) && !(double.IsInfinity(doubleValue) || double.IsNegativeInfinity(doubleValue)))
                    {
                        return doubleValue;
                    }

                    return null;
                case JsonValueKind.False:
                case JsonValueKind.True:
                    return element.GetBoolean();
                default:
                    return null;
            }
        }
    }
}
