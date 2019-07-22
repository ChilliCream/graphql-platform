using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Stitching.Utilities
{
    internal static class HttpResponseDeserializer
    {
        private const string _data = "data";
        private const string _extensions = "extensions";
        private const string _errors = "errors";

        public static QueryResult Deserialize(
            IReadOnlyDictionary<string, object> serializedResult)
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
            IReadOnlyDictionary<string, object> serializedResult,
            string field)
        {
            if (serializedResult.TryGetValue(field, out object o)
                && o is IReadOnlyDictionary<string, object> d)
            {
                foreach (KeyValuePair<string, object> item in d)
                {
                    data[item.Key] = item.Value;
                }
            }
        }

        private static void DeserializeErrors(
            IQueryResult result,
            IReadOnlyDictionary<string, object> serializedResult)
        {
            if (serializedResult.TryGetValue(_errors, out object o)
                && o is IReadOnlyList<object> l)
            {
                foreach (var error in
                    l.OfType<IReadOnlyDictionary<string, object>>())
                {
                    result.Errors.Add(
                        ErrorBuilder.FromDictionary(error).Build());
                }
            }
        }
    }
}
