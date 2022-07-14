using System;
using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using static HotChocolate.Execution.Properties.Resources;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

public static class ExecutionResultExtensions
{
    private static readonly JsonQueryResultFormatter _formatter = new(false);
    private static readonly JsonQueryResultFormatter _formatterIndented = new(true);

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
            _formatterIndented.Format(result, writer);
        }
        else
        {
            _formatter.Format(result, writer);
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

        if (result is IQueryResult queryResult)
        {
            return withIndentations
                ? _formatterIndented.Serialize(queryResult)
                : _formatter.Serialize(queryResult);
        }

        throw new NotSupportedException(ExecutionResultExtensions_OnlyQueryResults);
    }
}
