using HotChocolate.Pagination;

namespace GreenDonut;

public static class HotChocolatePaginationBatchingDataLoaderFetchContext
{
    public static PagingArguments GetPagingArguments<TValue>(this DataLoaderFetchContext<TValue> context)
        => context.GetRequiredState<PagingArguments>();
}
