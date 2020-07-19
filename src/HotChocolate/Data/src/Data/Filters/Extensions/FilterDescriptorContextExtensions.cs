using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public static class FilterDescriptorContetExtensions
    {
        public static IFilterConvention GetFilterConvention(
            this IDescriptorContext context,
            string? scope) =>
            context.GetConventionOrDefault(scope, FilterConvention.Default);
    }
}
