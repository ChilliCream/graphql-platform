using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public static class FilterConventionExtensions
    {
        public static IFilterConvention GetFilterConvention(
            this IDescriptorContext context)
            => context.GetConventionOrDefault(FilterConvention.Default);
    }
}
