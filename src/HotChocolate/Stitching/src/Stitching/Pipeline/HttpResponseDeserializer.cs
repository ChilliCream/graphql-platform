using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Pipeline
{
    internal static class HttpResponseDeserializer
    {
        private const string _data = "data";
        private const string _extensions = "extensions";
        private const string _errors = "errors";

        private static readonly ObjectValueToDictionaryConverter _converter =
            new ObjectValueToDictionaryConverter();

        public static IQueryResult Deserialize(
            IReadOnlyDictionary<string, object?> serializedResult)
        {
            var result = new QueryResultBuilder();

            if (serializedResult.TryGetValue(_data, out object? data))
            {
                result.SetData(data as IReadOnlyDictionary<string, object?>);
            }

            if (serializedResult.TryGetValue(_extensions, out object? extensionData))
            {
                result.SetExtensions(extensionData as IReadOnlyDictionary<string, object?>);
            }

            DeserializeErrors(result, serializedResult);

            return result.Create();
        }

        private static void DeserializeErrors(
            IQueryResultBuilder result,
            IReadOnlyDictionary<string, object?> serializedResult)
        {
            if (serializedResult.TryGetValue(_errors, out object? o)
                && o is ListValueNode l)
            {
                foreach (var error in l.Items.OfType<ObjectValueNode>())
                {
                    Dictionary<string, object?> dict = _converter.Convert(error);
                    result.AddError(ErrorBuilder.FromDictionary(dict).Build());
                }
            }
        }
    }
}
