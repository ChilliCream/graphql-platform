namespace HotChocolate.Execution;

public static class QueryCacheOperationRequestBuilderExtensions
{
    /// <summary>
    /// Skip the query result caching for the current request.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IOperationRequestBuilder"/>.
    /// </param>
    public static IOperationRequestBuilder SkipQueryCaching(
        this IOperationRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.SkipQueryCaching, null);
}
