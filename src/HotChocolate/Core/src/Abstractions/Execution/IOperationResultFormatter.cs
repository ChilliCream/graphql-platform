using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a formatter for <see cref="IOperationResult"/>s.
/// </summary>
public interface IOperationResultFormatter
{
    /// <summary>
    /// Formats a query result and writes the formatted result to
    /// the given <paramref name="outputStream"/>.
    /// </summary>
    /// <param name="result">
    /// The query result that shall be formatted.
    /// </param>
    /// <param name="outputStream">
    /// The stream to which the formatted <paramref name="result"/> shall be written to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// <paramref name="outputStream"/> is <c>null</c>.
    /// </exception>
    ValueTask FormatAsync(
        IOperationResult result,
        Stream outputStream,
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
        IOperationResult result,
        IBufferWriter<byte> writer);
}
