using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    public static class ProjectionDescriptorContextExtensions
    {
        public static IProjectionProvider GetProjectionConvention(
            this ITypeSystemObjectContext context,
            string? scope = null) =>
            context.DescriptorContext.GetProjectionConvention(scope);

        public static IProjectionProvider GetProjectionConvention(
            this IDescriptorContext context,
            string? scope = null) =>
            context.GetConventionOrDefault<IProjectionProvider>(
                () => throw new Exception(),
                scope);
    }
}
