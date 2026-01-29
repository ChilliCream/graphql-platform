using System.Buffers;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using static HotChocolate.Properties.Resources;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

public static class ExecutionResultExtensions
{
    private static readonly JsonResultFormatter s_formatter = JsonResultFormatter.Default;
    private static readonly JsonResultFormatter s_formatterIndented = JsonResultFormatter.Indented;

    public static void WriteTo(
        this OperationResult result,
        IBufferWriter<byte> writer,
        bool withIndentations = true)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        if (withIndentations)
        {
            s_formatterIndented.Format(result, writer);
        }
        else
        {
            s_formatter.Format(result, writer);
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
        ArgumentNullException.ThrowIfNull(result);

        if (result is OperationResult operationResult)
        {
            using var writer = new PooledArrayWriter();

            if (withIndentations)
            {
                s_formatterIndented.Format(operationResult, writer);
                return Encoding.UTF8.GetString(writer.WrittenSpan);
            }

            s_formatter.Format(operationResult, writer);
            return Encoding.UTF8.GetString(writer.WrittenSpan);
        }

        throw new NotSupportedException(ExecutionResultExtensions_OnlyQueryResults);
    }
}
