using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate;

public static class ExecutionResultExtensions
{
    private static readonly JsonQueryResultSerializer _serializer = new(false);
    private static readonly JsonQueryResultSerializer _serializerIndented = new(true);

    public static void WriteTo(
        this IQueryResult result,
        IBufferWriter<byte> writer,
        bool withIndentations = true)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (withIndentations)
        {
            _serializerIndented.Serialize(result, writer);
        }
        else
        {
            _serializer.Serialize(result, writer);
        }
    }

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
            return withIndentations
                ? _serializerIndented.Serialize(queryResult)
                : _serializer.Serialize(queryResult);
        }

        throw new NotSupportedException(ExecutionResultExtensions_OnlyQueryResults);
    }
}
