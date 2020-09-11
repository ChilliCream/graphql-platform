using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data.Sorting
{
    public static class MockConventionExtensions
    {
        public static ISortConventionDescriptor UseMock(
            this ISortConventionDescriptor descriptor)
        {
            return descriptor.AddDefaults().Provider(
                new QueryableSortProvider(x => x
                    .AddDefaultFieldHandlers()
                    .AddOperationHandler<MatchAnyQueryableOperationHandler>()
                    .AddFieldHandler<MatchAnyQueryableFieldHandler>()));
        }
    }
}
