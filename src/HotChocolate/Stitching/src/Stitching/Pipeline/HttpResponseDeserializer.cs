using System;
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
                && o is IReadOnlyList<object> errors)
            {
                foreach (var obj in errors)
                {
                    IError error = ErrorBuilder
                        .FromDictionary(DeserializeErrorObject(obj))
                        .Build();

                    result.AddError(error);
                }
            }
        }

        private static object? DeserializeErrorValue(object? value)
        {
            switch (value)
            {
                case IReadOnlyDictionary<string, object?> obj:
                    return DeserializeErrorObject(obj);

                case IReadOnlyList<object?> list:
                    return DeserializeErrorList(list);

                case StringValueNode sv:
                    return sv.Value;

                case EnumValueNode ev:
                    return ev.Value;

                case IntValueNode iv:
                    return iv.ToInt32();

                case FloatValueNode fv:
                    return fv.ToDouble();

                case BooleanValueNode bv:
                    return bv.Value;

                case NullValueNode:
                case null:
                    return null;

                default:
                    throw new NotSupportedException();
            }
        }

        private static Dictionary<string, object?> DeserializeErrorObject(
            object obj)
        {
            if (obj is IReadOnlyDictionary<string, object?> dict)
            {
                return DeserializeErrorObject(dict);
            }

            throw new NotSupportedException("An error object must be a dictionary.");
        }

        private static Dictionary<string, object?> DeserializeErrorObject(
            IReadOnlyDictionary<string, object?> obj)
        {
            var deserialized = new Dictionary<string, object?>();

            foreach (var item in obj)
            {
                deserialized.Add(item.Key, DeserializeErrorValue(item.Value));
            }

            return deserialized;
        }

        private static List<object?> DeserializeErrorList(
            IReadOnlyList<object?> list)
        {
            var deserialized = new List<object?>();

            foreach (var item in list)
            {
                deserialized.Add(DeserializeErrorValue(item));
            }

            return deserialized;
        }
    }
}
