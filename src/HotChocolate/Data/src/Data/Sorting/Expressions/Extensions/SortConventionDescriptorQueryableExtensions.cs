using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data;

public static class SortConventionDescriptorQueryableExtensions
{
    public static ISortConventionDescriptor UseQueryableProvider(
        this ISortConventionDescriptor descriptor) =>
        descriptor.Provider(new QueryableSortProvider(x => x.AddDefaultFieldHandlers()));

    public static ISortProviderDescriptor<QueryableSortContext> AddDefaultFieldHandlers(
        this ISortProviderDescriptor<QueryableSortContext> descriptor)
    {
        descriptor.AddOperationHandler(QueryableAscendingSortOperationHandler.Create);
        descriptor.AddOperationHandler(QueryableDescendingSortOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableDefaultSortFieldHandler.Create);
        return descriptor;
    }
}
