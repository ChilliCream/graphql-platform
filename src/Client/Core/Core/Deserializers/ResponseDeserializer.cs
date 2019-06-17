using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Client.Core.Deserializers
{
    public class ResponseDeserializer
    {
        public JObject Deserialize(string data)
        {
            var result = JObject.Parse(data);

            if (result["errors"] != null)
            {
                throw DeserializeExceptions((JArray)result["errors"]);
            }

            return result;
        }

        public TResult Deserialize<TResult>(SimpleQuery<TResult> query, string data)
        {
            return Deserialize(query, JObject.Parse(data));
        }

        public TResult Deserialize<TResult>(SimpleQuery<TResult> query, JObject data)
        {
            if (data["errors"] != null)
            {
                throw DeserializeExceptions((JArray)data["errors"]);
            }

            return query.ResultBuilder(data);
        }

        public TResult Deserialize<TResult>(Func<JObject, TResult> deserialize, string data)
        {
            return Deserialize(deserialize, JObject.Parse(data));
        }

        public TResult Deserialize<TResult>(Func<JObject, TResult> deserialize, JObject data)
        {
            if (data["errors"] != null)
            {
                throw DeserializeExceptions((JArray)data["errors"]);
            }

            return deserialize(data);
        }

        private Exception DeserializeExceptions(JArray errors)
        {
            if (errors.Count == 1)
            {
                return DeserializeException(errors[0]);
            }
            else
            {
                var inner = Enumerable.Select(errors, x => DeserializeException(x)).ToList();
                return new AggregateException(inner);
            }
        }

        private Exception DeserializeException(JToken error)
        {
            return new ResponseDeserializerException(
                (string)error["message"],
                (int)error["locations"][0]["line"],
                (int)error["locations"][0]["column"]);
        }
    }
}
