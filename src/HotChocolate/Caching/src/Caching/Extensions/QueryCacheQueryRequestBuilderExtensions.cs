namespace HotChocolate.Execution;

public static class QueryCacheOperationRequestBuilderExtensions
{
    /// <summary>
    /// Skip the query result caching for the current request.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="OperationRequestBuilder"/>.
    /// </param>
    public static OperationRequestBuilder SkipQueryCaching(
        this OperationRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.SkipQueryCaching, null);
}
