using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Filters.Spatial;

namespace HotChocolate.Data
{
    public static class SpatialFilterProviderDescriptorQueryableExtensions
    {
        public static IFilterProviderDescriptor<QueryableFilterContext> AddSpatialHandlers(
            this IFilterProviderDescriptor<QueryableFilterContext> descriptor) =>
            descriptor
                .AddFieldHandler<QueryableSpatialGeometryDataHandler>()
                .AddFieldHandler<QueryableSpatialBufferDataHandler>()
                .AddFieldHandler<QueryableSpatialContainsOperationHandler>()
                .AddFieldHandler<QueryableSpatialNotContainsOperationHandler>()
                .AddFieldHandler<QueryableSpatialDistanceOperationHandler>()
                .AddFieldHandler<QueryableSpatialIntersectsOperationHandler>()
                .AddFieldHandler<QueryableSpatialNotIntersectsOperationHandler>()
                .AddFieldHandler<QueryableSpatialOverlapsOperationHandler>()
                .AddFieldHandler<QueryableSpatialNotOverlapsOperationHandler>()
                .AddFieldHandler<QueryableSpatialTouchesOperationHandler>()
                .AddFieldHandler<QueryableSpatialNotTouchesOperationHandler>()
                .AddFieldHandler<QueryableSpatialWithinOperationHandler>()
                .AddFieldHandler<QueryableSpatialNotWithinOperationHandler>();
    }
}
