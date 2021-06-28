namespace HotChocolate.Data.Projections.Spatial
{
    public static class SpatialProjectionProviderDescriptorQueryableExtensions
    {
        public static IProjectionProviderDescriptor AddSpatialHandlers(
            this IProjectionProviderDescriptor descriptor) =>
            descriptor.RegisterFieldHandler<QueryableSpatialProjectionScalarHandler>();
    }
}
