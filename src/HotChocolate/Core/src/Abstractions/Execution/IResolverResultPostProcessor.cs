namespace HotChocolate.Execution;

/// <summary>
/// <para>
/// The result post processor allows to post process the result of the resolver pipeline
/// before the value goes through the GraphQL value completion.
/// </para>
/// <para>
/// This is useful if you want to complete IO operations like reading the content of a file streams
/// into memory so that it can go through the value completion or other async operations.
/// </para>
/// </summary>
public interface IResolverResultPostProcessor
{
    /// <summary>
    /// Post processes the result so that it can be completed by the GraphQL value completion.
    /// </summary>
    /// <param name="result">
    /// The result of the resolver pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the processed result.
    /// </returns>
    ValueTask<object?> ToCompletionResultAsync(
        object result,
        CancellationToken cancellationToken);

    /// <summary>
    /// Post processes the result to an async enumerable that can be used to stream the result.
    /// </summary>
    /// <param name="result">
    /// The result of the resolver pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns an IAsyncEnumerable that represents the result stream.
    /// </returns>
    IAsyncEnumerable<object?> ToStreamResultAsync(
        object result,
        CancellationToken cancellationToken);
}
