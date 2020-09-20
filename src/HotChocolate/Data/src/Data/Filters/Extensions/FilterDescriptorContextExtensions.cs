using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Filters
{
    public static class FilterDescriptorContextExtensions
    {
        public static IFilterConvention GetFilterConvention(
            this ITypeSystemObjectContext context,
            string? scope = null) =>
            context.DescriptorContext.GetFilterConvention(scope);

        public static IFilterConvention GetFilterConvention(
            this IDescriptorContext context,
            string? scope = null) =>
            context.GetConventionOrDefault<IFilterConvention>(
                () => throw FilterDescriptorContextExtensions_NoConvention(scope),
                scope);
    }
}
