using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data
{
    public static class SortDescriptorContextExtensions
    {
        public static ISortConvention GetSortConvention(
            this ITypeSystemObjectContext context,
            string? scope = null) =>
            context.DescriptorContext.GetSortConvention(scope);

        public static ISortConvention GetSortConvention(
            this IDescriptorContext context,
            string? scope = null) =>
            context.GetConventionOrDefault<ISortConvention>(
                () => throw SortDescriptorContextExtensions_NoConvention(scope),
                scope);
    }
}
