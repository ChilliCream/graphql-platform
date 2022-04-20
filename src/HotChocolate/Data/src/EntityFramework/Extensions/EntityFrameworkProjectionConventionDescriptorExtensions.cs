using HotChocolate.Data.Projections;

namespace HotChocolate.Data.Extensions;

internal static class EntityFrameworkProjectionConventionDescriptorExtensions
{
    public static IProjectionConventionDescriptor AddEntityFrameworkDefaults(
        this IProjectionConventionDescriptor descriptor) =>
        descriptor.Provider(new EntityFrameworkQueryableProjectionProvider(x => x.AddDefaults()));
}
