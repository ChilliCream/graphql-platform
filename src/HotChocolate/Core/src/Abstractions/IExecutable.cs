using System.Collections;

namespace HotChocolate;

/// <summary>
/// Represents a query that can be executed against a data source.
/// </summary>
public interface IExecutable
{
    /// <summary>
    /// The current state of the executable
    /// </summary>
    object Source { get; }

    /// <summary>
    /// Executes the executable and returns a list
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>Returns an arbitrary list</returns>
    ValueTask<IList> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the executable and returns an async enumerable.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>Returns an async enumerable</returns>
    IAsyncEnumerable<object?> ToAsyncEnumerable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no
    /// elements.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>Returns the result</returns>
    ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the only element of a default value if no such element exists. This method
    /// throws an exception if more than one element satisfies the condition.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of elements in the sequence.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>
    /// The number of elements in the sequence.
    /// </returns>
    ValueTask<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints the underlying query.
    /// </summary>
    /// <returns>A string that represents the underlying query.</returns>
    string Print();
}
