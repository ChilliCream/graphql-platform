using System;
using HotChocolate.Execution;
using HotChocolate.Properties;

namespace HotChocolate
{
    public static class ExecutionResultExtensions
    {
        private static JsonQueryResultSerializer _serializer =
            new JsonQueryResultSerializer(false);
        private static JsonQueryResultSerializer _serializerWithIndent =
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
                    return _serializerWithIndent.Serialize(queryResult);
                }
                return _serializer.Serialize(queryResult);
            }

            throw new NotSupportedException(
                CoreResources.ToJson_OnlyQueryResultsSupported);
        }
    }
}
