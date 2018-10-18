using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    public static class QueryMiddlewareUtilities
    {
        public static Dictionary<string, object> DeserializeVariables(
        JObject input)
        {
            if (input == null)
            {
                return null;
            }

            return DeserializeVariables(input.ToObject<Dictionary<string, JToken>>());
        }

        private static Dictionary<string, object> DeserializeVariables(
            Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            var values = new Dictionary<string, object>();
            foreach (string key in input.Keys.ToArray())
            {
                values[key] = DeserializeVariableValue(input[key]);
            }
            return values;
        }

        private static ObjectValueNode DeserializeObjectValue(
           Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            var fields = new List<ObjectFieldNode>();
            foreach (string key in input.Keys.ToArray())
            {
                fields.Add(new ObjectFieldNode(null,
                    new NameNode(null, key),
                    DeserializeVariableValue(input[key])));
            }
            return new ObjectValueNode(null, fields);
        }

        private static IValueNode DeserializeVariableValue(object value)
        {
            if (value is JObject jo)
            {
                return DeserializeObjectValue(
                    jo.ToObject<Dictionary<string, JToken>>());
            }

            if (value is JArray ja)
            {
                return DeserializeVariableListValue(ja);
            }

            if (value is JValue jv)
            {
                return DeserializeVariableScalarValue(jv);
            }

            throw new NotSupportedException();
        }

        private static IValueNode DeserializeVariableListValue(JArray array)
        {
            var list = new List<IValueNode>();
            foreach (JToken token in array.Children())
            {
                list.Add(DeserializeVariableValue(token));
            }
            return new ListValueNode(null, list);
        }

        private static IValueNode DeserializeVariableScalarValue(JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return new BooleanValueNode(value.Value<bool>());
                case JTokenType.Integer:
                    return new IntValueNode(value.Value<string>());
                case JTokenType.Float:
                    return new FloatValueNode(value.Value<string>());
                default:
                    return new StringValueNode(value.Value<string>());
            }
        }

        public static IServiceProvider CreateRequestServices(HttpContext context)
        {
            Dictionary<Type, object> services = new Dictionary<Type, object>
            {
                { typeof(HttpContext), context }
            };

            return new RequestServiceProvider(
                context.RequestServices, services);
        }
    }
}
