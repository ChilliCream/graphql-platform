using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Pipeline
{
    internal static class HttpResponseDeserializer
    {
        private const string _data = "data";
        private const string _extensions = "extensions";
        private const string _errors = "errors";

        public static IQueryResult Deserialize(
            IReadOnlyDictionary<string, object> serializedResult)
        {
            var result = new QueryResultBuilder();
            var data = new OrderedDictionary();
            var extensionData = new ExtensionData();

            DeserializeRootField(
                data,
                serializedResult,
                _data);
            result.SetData(data);

            DeserializeRootField(
                extensionData,
                serializedResult,
                _extensions);
            result.SetExtensions(extensionData);

            DeserializeErrors(result, serializedResult);

            return result.Create();
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
            IQueryResultBuilder result,
            IReadOnlyDictionary<string, object> serializedResult)
        {
            if (serializedResult.TryGetValue(_errors, out object o)
                && o is IReadOnlyList<object> l)
            {
                foreach (var error in l.OfType<IReadOnlyDictionary<string, object>>())
                {
                    result.AddError(ErrorBuilder.FromDictionary(error).Build());
                }
            }
        }
    }
}
