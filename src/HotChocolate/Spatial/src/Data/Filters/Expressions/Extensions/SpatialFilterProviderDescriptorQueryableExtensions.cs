using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters.Spatial
{
    public static class SpatialFilterProviderDescriptorQueryableExtensions
    {
        public static IFilterProviderDescriptor<QueryableFilterContext> AddSpatialHandlers(
            this IFilterProviderDescriptor<QueryableFilterContext> descriptor) =>
            descriptor
                .AddFieldHandler<QueryableSpatialGeometryDataHandler>()
                .AddFieldHandler<QueryableSpatialBufferDataHandler>()
                .AddFieldHandler<QueryableSpatialContainsOperationHandler>()
                .AddFieldHandler<QueryableSpatialDistanceOperationHandler>()
                .AddFieldHandler<QueryableSpatialIntersectsOperationHandler>()
                .AddFieldHandler<QueryableSpatialOverlapsOperationHandler>()
                .AddFieldHandler<QueryableSpatialTouchesOperationHandler>()
                .AddFieldHandler<QueryableSpatialWithinOperationHandler>();
    }
}
