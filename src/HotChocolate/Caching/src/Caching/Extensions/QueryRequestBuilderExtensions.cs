using HotChocolate.Execution;

namespace HotChocolate.Caching;

public static class QueryRequestBuilderExtensions
{
    public static IQueryRequestBuilder SkipQueryCaching(
        this IQueryRequestBuilder builder) =>
        builder.SetProperty(WellKnownContextData.SkipQueryCaching, null);
}