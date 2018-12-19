using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    internal static class QueryMiddlewareUtilities
    {
        public static Dictionary<string, object> ToDictionary(
            this JObject input)
        {
            if (input == null)
            {
                return null;
            }

            return ToDictionary(
                input.ToObject<Dictionary<string, JToken>>());
        }

        private static Dictionary<string, object> ToDictionary(
            Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            var values = new Dictionary<string, object>();

            foreach (string key in input.Keys.ToArray())
            {
                values[key] = DeserializeValue(input[key]);
            }

            return values;
        }

        private static Dictionary<string, object> DeserializeObject(
            Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            var fields = new Dictionary<string, object>();

            foreach (string key in input.Keys.ToArray())
            {
                fields[key] = DeserializeValue(input[key]);
            }

            return fields;
        }

        private static object DeserializeValue(object value)
        {
            if (value is JObject jo)
            {
                return DeserializeObject(
                    jo.ToObject<Dictionary<string, JToken>>());
            }

            if (value is JArray ja)
            {
                return DeserializeList(ja);
            }

            if (value is JValue jv)
            {
                return DeserializeScalar(jv);
            }

            throw new NotSupportedException();
        }

        private static List<object> DeserializeList(JArray array)
        {
            var list = new List<object>();

            foreach (JToken token in array.Children())
            {
                list.Add(DeserializeValue(token));
            }

            return list;
        }

        private static object DeserializeScalar(JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return value.Value<bool>();

                case JTokenType.Integer:
                    return value.Value<int>();

                case JTokenType.Float:
                    return value.Value<double>();

                default:
                    return value.Value<string>();
            }
        }
    }
}
