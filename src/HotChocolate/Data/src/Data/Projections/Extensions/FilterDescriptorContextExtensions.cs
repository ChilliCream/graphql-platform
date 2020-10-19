using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    public static class ProjectionDescriptorContextExtensions
    {
        public static IProjectionConvention GetProjectionConvention(
            this ITypeSystemObjectContext context,
            string? scope = null) =>
            context.DescriptorContext.GetProjectionConvention(scope);

        public static IProjectionConvention GetProjectionConvention(
            this IDescriptorContext context,
            string? scope = null) =>
            context.GetConventionOrDefault<IProjectionConvention>(
                // TODO : this need a better exception.
                () => throw new Exception("Projection provider not found."),
                scope);
    }
}
