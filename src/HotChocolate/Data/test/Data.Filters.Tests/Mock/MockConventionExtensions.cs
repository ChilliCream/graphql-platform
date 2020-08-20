using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters
{
    public static class MockConventionExtensions
    {
        public static IFilterConventionDescriptor UseMock(
            this IFilterConventionDescriptor descriptor)
        {
            return descriptor.UseDefault().Provider(
                 new QueryableFilterProvider(
                     x => x
                         .AddDefaultFieldHandlers()
                         .AddFieldHandler<MatchAnyQueryableFieldHandler>()));
        }
    }
}
