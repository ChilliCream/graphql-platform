using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Spatial.Filters
{
    public static class SpatialFilterProviderDescriptorQueryableExtensions
    {
        public static IFilterProviderDescriptor<QueryableFilterContext> AddSpatialHandlers(
            this IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        {
            descriptor.AddFieldHandler<QueryableSpatialBufferDataHandler>();
            descriptor.AddFieldHandler<QueryableSpatialGeometryDataHandler>();
            descriptor.AddFieldHandler<QueryableSpatialContainsOperationHandler>();
            descriptor.AddFieldHandler<QueryableSpatialDistanceOperationHandler>();
            return descriptor;
        }
    }
}
