using HotChocolate.Pagination;

namespace GreenDonut.Projections;

public static class PaginationBatchingDataLoaderFetchContext
{
    public static PagingArguments GetPagingArguments<TValue>(this DataLoaderFetchContext<TValue> context)
        => context.GetRequiredState<PagingArguments>();
}
