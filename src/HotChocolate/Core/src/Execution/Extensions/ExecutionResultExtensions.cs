using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;

namespace HotChocolate
{
    public static class ExecutionResultExtensions
    {
        private static readonly JsonQueryResultSerializer _serializer =
            new JsonQueryResultSerializer(false);
        private static readonly JsonArrayResponseStreamSerializer _streamSerializer =
            new JsonArrayResponseStreamSerializer();
        private static readonly JsonQueryResultSerializer _serializerIndented =
            new JsonQueryResultSerializer(true);

        public static string ToJson(
            this IExecutionResult result,
            bool withIndentations = true)
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

            // TODO : resources / throw helper
            throw new NotSupportedException(
                "Only query results are supported.");
        }

        public static async ValueTask<string> ToJsonAsync(
            this IExecutionResult result,
            bool withIndentations = true)
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

            if (result is IResponseStream responseStream)
            {
                // TODO : lets rework the serializer to align it with the query result serializer
                using (var stream = new MemoryStream())
                {
                    await _streamSerializer
                        .SerializeAsync(responseStream, stream)
                        .ConfigureAwait(false);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            // TODO : resources / throw helper
            throw new NotSupportedException(
                "Only query results are supported.");
        }
    }
}
