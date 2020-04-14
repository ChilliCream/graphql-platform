using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    public static class SortingConventionExtension
    {
        public static ISortingConvention GetSortingConvention(
           this IDescriptorContext context)
        {
            return context.GetConventionOrDefault(SortingConvention.Default);
        }
    }
}
