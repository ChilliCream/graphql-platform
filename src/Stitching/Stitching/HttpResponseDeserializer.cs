using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Stitching
{
    internal static class HttpResponseDeserializer
    {
        private const string _data = "data";
        private const string _extensions = "extensions";
        private const string _errors = "errors";

        public static QueryResult Deserialize(
            JObject serializedResult)
        {
            var result = new QueryResult();

            DeserializeRootField(
                result.Data,
                serializedResult,
                _data);

            DeserializeRootField(
                result.Extensions,
                serializedResult,
                _extensions);

            DeserializeErrors(result, serializedResult);

            return result;
        }

        private static void DeserializeRootField(
            IDictionary<string, object> data,
            JObject serializedResult,
            string field)
        {
            if (serializedResult.Property(field)?.Value is JObject obj)
            {
                foreach (KeyValuePair<string, object> item in
                    DeserializeObject(obj))
                {
                    data[item.Key] = item.Value;
                }
            }
        }

        private static void DeserializeErrors(
            QueryResult result,
            JObject serializedResult)
        {
            if (serializedResult.Property(_errors)?.Value is JArray array)
            {
                foreach (JObject error in array.Children().OfType<JObject>())
                {
                    result.Errors.Add(
                        QueryError.FromDictionary(
                            DeserializeObject(error)));
                }
            }
        }

        private static object DeserializeToken(JToken token)
        {
            switch (token)
            {
                case JObject o:
                    return DeserializeObject(o);
                case JArray a:
                    return DeserializeList(a);
                case JValue v:
                    return DeserializeScalar(v);
                default:
                    throw new NotSupportedException();
            }
        }

        private static OrderedDictionary DeserializeObject(JObject obj)
        {
            var dict = new OrderedDictionary();

            foreach (JProperty property in obj.Properties())
            {
                dict[property.Name] = DeserializeToken(property.Value);
            }

            return dict;
        }

        private static List<object> DeserializeList(JArray array)
        {
            var list = new List<object>();

            foreach (JToken token in array.Children())
            {
                list.Add(DeserializeToken(token));
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
                    return value.Value<long>();

                case JTokenType.Float:
                    return value.Value<decimal>();

                default:
                    return value.Value<string>();
            }
        }
    }
}
