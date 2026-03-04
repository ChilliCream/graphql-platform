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
    /// <param name="flags">
    /// The flags that control the formatting behavior.
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
        ExecutionResultFormatFlags flags = ExecutionResultFormatFlags.None,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Flags that control how an <see cref="IExecutionResult"/> is formatted.
/// </summary>
[Flags]
public enum ExecutionResultFormatFlags
{
    /// <summary>
    /// No special formatting behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Formats incremental payloads using the RFC-1 compatibility shape.
    /// </summary>
    IncrementalRfc1 = 1 << 0
}
