using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public static class FilterConventionExtensions
    {
        public static IFilterConvention GetFilterConvention(
            this IDescriptorContext context)
        {
            return context.GetConventionOrDefault(FilterConvention.Default);
        }
    }
}