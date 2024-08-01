namespace HotChocolate;

public interface IExecutable<T> : IExecutable
{
    /// <summary>
    /// Executes the executable and returns a list
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>Returns a arbitrary list</returns>
    new ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the executable and returns an async enumerable.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>Returns an async enumerable</returns>
    new IAsyncEnumerable<T> ToAsyncEnumerable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no
    /// elements.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>Returns the result</returns>
    new ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the only element of a default value if no such element exists. This method
    /// throws an exception if more than one element satisfies the condition.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution.
    /// </param>
    /// <returns>
    /// The single element of the input sequence, or default(T) if the sequence contains no
    /// </returns>
    new ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);
}
