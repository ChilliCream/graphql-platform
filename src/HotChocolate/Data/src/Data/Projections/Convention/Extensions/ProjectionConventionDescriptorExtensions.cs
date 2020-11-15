using HotChocolate.Data.Projections.Expressions;

namespace HotChocolate.Data.Projections
{
    public static class ProjectionConventionDescriptorExtensions
    {
        public static IProjectionConventionDescriptor AddDefaults(
            this IProjectionConventionDescriptor descriptor) =>
            descriptor.Provider(new QueryableProjectionProvider(x => x.AddDefaults()));
    }
}
