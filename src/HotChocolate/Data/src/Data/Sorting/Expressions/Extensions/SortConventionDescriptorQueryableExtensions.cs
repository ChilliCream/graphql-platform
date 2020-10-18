using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data.Sorting
{
    public static class SortConventionDescriptorQueryableExtensions
    {
        public static ISortConventionDescriptor UseQueryableProvider(
            this ISortConventionDescriptor descriptor) =>
            descriptor.Provider(new QueryableSortProvider(x => x.AddDefaultFieldHandlers()));

        public static ISortProviderDescriptor<QueryableSortContext> AddDefaultFieldHandlers(
            this ISortProviderDescriptor<QueryableSortContext> descriptor)
        {
            descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
            descriptor.AddOperationHandler<QueryableDescendingSortOperationHandler>();
            descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            return descriptor;
        }
    }
}
