using System.IO.Pipelines;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a formatter for <see cref="IExecutionResult"/>s.
/// </summary>
public interface IExecutionResultFormatter
{
    /// <summary>
    /// Formats an execution result and writes the formatted result to
    /// the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="result">
    /// The execution result that shall be formatted.
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
        IExecutionResult result,
        PipeWriter writer,
        CancellationToken cancellationToken = default);
}
