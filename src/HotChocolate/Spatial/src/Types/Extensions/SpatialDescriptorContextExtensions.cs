using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial.Configuration;

namespace HotChocolate.Types.Spatial
{
    /// <summary>
    /// Common extensions of descriptors for spatial types
    /// </summary>
    public static class SpatialDescriptorContextExtensions
    {
        /// <summary>
        /// Gets the <see cref="SpatialConvention"/> of this schema
        /// </summary>
        /// <param name="context">The type completion context</param>
        /// <returns>the spatial convention</returns>
        public static ISpatialConvention GetSpatialConvention(
            this ITypeCompletionContext context) =>
            context.DescriptorContext.GetSpatialConvention();

        /// <summary>
        /// Gets the <see cref="SpatialConvention"/> of this schema
        /// </summary>
        /// <param name="context">The descriptor completion context</param>
        /// <returns>the spatial convention</returns>
        public static ISpatialConvention GetSpatialConvention(this IDescriptorContext context) =>
            context.GetConventionOrDefault<ISpatialConvention>(() =>
                throw new InvalidOperationException());
    }
}
