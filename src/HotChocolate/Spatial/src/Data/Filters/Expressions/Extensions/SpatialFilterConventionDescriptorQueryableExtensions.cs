using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Spatial.Filters
{
    public static class SpatialFilterConventionDescriptorQueryableExtensions
    {
        public static IFilterProviderDescriptor<QueryableFilterContext> AddSpatialHandlers(
            this IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        {
            descriptor.AddFieldHandler<QueryableSpatialBufferDataHandler>();
            descriptor.AddFieldHandler<QueryableSpatialToDataHandler>();
            descriptor.AddFieldHandler<QueryableSpatialGeometryDataHandler>();

            descriptor.AddFieldHandler<QueryableSpatialContainsOperationHandler>();
            return descriptor;
        }
    }
}
