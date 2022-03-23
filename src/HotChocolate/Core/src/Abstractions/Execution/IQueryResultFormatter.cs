using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Represents a formatter for <see cref="IQueryResult"/>s.
/// </summary>
public interface IQueryResultFormatter
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
    Task FormatAsync(
        IQueryResult result,
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
    void Format(
        IQueryResult result,
        IBufferWriter<byte> writer);
}
