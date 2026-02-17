using System.Buffers;
using System.IO.Pipelines;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a formatter for <see cref="OperationResult"/>s.
/// </summary>
public interface IOperationResultFormatter
{
    /// <summary>
    /// Formats a query result and writes the formatted result to
    /// the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="result">
    /// The query result that shall be formatted.
    /// </param>
    /// <param name="writer">
    /// The pipe writer.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    ValueTask FormatAsync(
        OperationResult result,
        PipeWriter writer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats a query result and writes the formatted result to
    /// the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="result">
    /// The query result that shall be formatted.
    /// </param>
    /// <param name="writer">
    /// The writer to which the formatted <paramref name="result"/> shall be written to.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    void Format(
        OperationResult result,
        IBufferWriter<byte> writer);
}
