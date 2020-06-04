using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using HotChocolate.Properties;
using Newtonsoft.Json;

namespace HotChocolate
{
    public static class ExecutionResultExtensions
    {
        private static readonly JsonQueryResultSerializer _serializer =
            new JsonQueryResultSerializer(false);
        private static readonly JsonQueryResultSerializer _serializerIndented =
            new JsonQueryResultSerializer(true);

        public static string ToJson(
            this IExecutionResult result) =>
            ToJson(result, true);

        public static string ToJson(
            this IExecutionResult result,
            bool withIndentations)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result is IReadOnlyQueryResult queryResult)
            {
                if (withIndentations)
                {
                    return _serializerIndented.Serialize(queryResult);
                }
                return _serializer.Serialize(queryResult);
            }

            // TODO : resources
            throw new NotSupportedException(
                "Only query results are supported.");
        }
    }
}
