using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a formatter for <see cref="IResponseStream"/>s.
/// </summary>
public interface IResponseStreamFormatter
{
    /// <summary>
    /// Formats the results from the response stream and
    /// writes them to the <paramref name="outputStream"/>.
    /// </summary>
    /// <param name="responseStream">
    /// The GraphQL response stream.
    /// </param>
    /// <param name="outputStream">
    /// The output stream to which the formatter results shall be written to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task FormatAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}
