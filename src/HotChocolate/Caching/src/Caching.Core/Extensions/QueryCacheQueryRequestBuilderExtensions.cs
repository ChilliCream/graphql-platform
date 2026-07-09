namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="OperationRequestBuilder"/>
/// to control query result caching behavior.
/// </summary>
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
