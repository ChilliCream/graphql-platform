using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public static class FilterNamingConventionExtensions
    {
        public static IFilterNamingConvention GetFilterNamingConvention(
            this IDescriptorContext context)
        {
            return context.GetConventionOrDefault(FilterNamingConventionBase.Default);
        }
    }
}
