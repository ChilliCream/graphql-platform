using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Projections;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
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
