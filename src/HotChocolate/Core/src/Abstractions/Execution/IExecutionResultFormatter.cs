namespace HotChocolate.Execution;

/// <summary>
/// Represents a formatter for <see cref="IExecutionResult"/>s.
/// </summary>
public interface IExecutionResultFormatter
{
    /// <summary>
    /// Formats a execution result and writes the formatted result to
    /// the given <paramref name="outputStream"/>.
    /// </summary>
    /// <param name="result">
    /// The execution result that shall be formatted.
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
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}
