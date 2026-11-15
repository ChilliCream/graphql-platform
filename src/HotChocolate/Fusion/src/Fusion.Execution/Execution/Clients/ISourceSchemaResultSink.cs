namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Receives the results of a source schema request in the order they are read from the response.
/// </summary>
internal interface ISourceSchemaResultSink
{
    /// <summary>
    /// Receives a result for the request at <paramref name="requestIndex"/> and takes ownership of it.
    /// The index is always <c>0</c> for a request that is not part of a batch.
    /// </summary>
    /// <param name="requestIndex">The index of the request in its batch.</param>
    /// <param name="result">The result, which the sink must dispose or hand on.</param>
    /// <returns><c>true</c> to continue reading the response; <c>false</c> to stop reading it.</returns>
    bool OnResult(int requestIndex, SourceSchemaResult result);
}
