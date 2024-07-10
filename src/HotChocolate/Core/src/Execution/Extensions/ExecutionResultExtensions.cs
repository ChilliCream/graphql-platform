using System;
using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using static HotChocolate.Execution.Properties.Resources;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

public static class ExecutionResultExtensions
{
    private static readonly JsonResultFormatter _formatter = new(new() { Indented = false, });
    private static readonly JsonResultFormatter _formatterIndented = new(new() { Indented = true, });

    public static void WriteTo(
        this IOperationResult result,
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

    /// <summary>
    /// Converts the <see cref="IExecutionResult"/> to a JSON string.
    /// </summary>
    /// <param name="result">
    /// The execution result.
    /// </param>
    /// <param name="withIndentations">
    /// Defines if the JSON should be formatted with indentations.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The execution result is not a query result.
    /// </exception>
    public static string ToJson(
        this IExecutionResult result,
        bool withIndentations = true)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result is IOperationResult queryResult)
        {
            return withIndentations
                ? _formatterIndented.Format(queryResult)
                : _formatter.Format(queryResult);
        }

        throw new NotSupportedException(ExecutionResultExtensions_OnlyQueryResults);
    }
}
