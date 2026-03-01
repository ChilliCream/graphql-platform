namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the response received from a source schema after executing a GraphQL operation.
/// The response carries transport metadata and provides access to the result stream.
/// </summary>
public abstract class SourceSchemaClientResponse : IDisposable
{
    /// <summary>
    /// Gets the URI that the request was sent to.
    /// </summary>
    public abstract Uri Uri { get; }

    /// <summary>
    /// Gets the content type of the response (e.g. <c>application/json</c>).
    /// </summary>
    public abstract string ContentType { get; }

    /// <summary>
    /// Gets whether the transport-level response indicates success (e.g. HTTP 2xx).
    /// </summary>
    public abstract bool IsSuccessful { get; }

    /// <summary>
    /// Reads the response as an asynchronous stream of <see cref="SourceSchemaResult"/> items.
    /// For non-batched responses this yields a single result; for batched or streamed responses
    /// it may yield multiple results.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel reading.
    /// </param>
    /// <returns>
    /// An async enumerable of source schema results.
    /// </returns>
    public abstract IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to read the response as a single result without creating an async stream.
    /// Returns <c>null</c> when the response must be read as a stream.
    /// </summary>
    public virtual ValueTask<SourceSchemaResult?> ReadAsSingleResultAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<SourceSchemaResult?>(null);

    /// <summary>
    /// Releases transport resources held by this response.
    /// </summary>
    public abstract void Dispose();
}
