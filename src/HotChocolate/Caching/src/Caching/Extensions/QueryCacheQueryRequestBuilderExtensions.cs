namespace HotChocolate.Execution;

public static class QueryCacheQueryRequestBuilderExtensions
{
    /// <summary>
    /// Skip the query result caching for the current request.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IQueryRequestBuilder"/>.
    /// </param>
    public static IQueryRequestBuilder SkipQueryCaching(
        this IQueryRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.SkipQueryCaching, null);
}
