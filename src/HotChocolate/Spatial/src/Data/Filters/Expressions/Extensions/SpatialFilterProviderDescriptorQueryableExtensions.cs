using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Filters.Spatial;

namespace HotChocolate.Data;

public static class SpatialFilterProviderDescriptorQueryableExtensions
{
    public static IFilterProviderDescriptor<QueryableFilterContext> AddSpatialHandlers(
        this IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler(QueryableSpatialGeometryDataHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialBufferDataHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialContainsOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialNotContainsOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialDistanceOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialIntersectsOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialNotIntersectsOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialOverlapsOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialNotOverlapsOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialTouchesOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialNotTouchesOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialWithinOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableSpatialNotWithinOperationHandler.Create);

        return descriptor;
    }
}
