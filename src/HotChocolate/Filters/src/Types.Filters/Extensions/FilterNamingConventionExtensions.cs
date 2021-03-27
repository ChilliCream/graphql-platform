using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public static class FilterNamingConventionExtensions
    {
        [Obsolete("Use HotChocolate.Data.")]
        public static IFilterNamingConvention GetFilterNamingConvention(
            this IDescriptorContext context)
        {
            return context.GetConventionOrDefault(FilterNamingConventionBase.Default);
        }
    }
}
