using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public static class FilterDescriptorContextExtensions
    {
        public static IFilterConvention GetFilterConvention(
            this IDescriptorContext context,
            string? scope)
            => context.GetConventionOrDefault(FilterConvention.Default);
    }
}
