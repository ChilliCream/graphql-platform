using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Spatial.Configuration
{
    public static class SpatialDescriptorContextExtensions
    {
        public static ISpatialConvention GetSpatialConvention(
            this ITypeCompletionContext context) =>
            context.DescriptorContext.GetSpatialConvention();

        public static ISpatialConvention GetSpatialConvention(this IDescriptorContext context) =>
            context.GetConventionOrDefault<ISpatialConvention>(() =>
                throw new InvalidOperationException());
    }
}
