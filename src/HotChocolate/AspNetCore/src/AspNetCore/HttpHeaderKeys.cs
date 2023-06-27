namespace HotChocolate.AspNetCore;

internal static class HttpHeaderKeys
{
    public const string Tracing = "GraphQL-Tracing";

    public const string ApolloTracing = "X-Apollo-Tracing";

    public const string QueryPlan = "GraphQL-Query-Plan";

    public const string CacheControl = "Cache-Control";

    public const string Preflight = "GraphQL-Preflight";
}
