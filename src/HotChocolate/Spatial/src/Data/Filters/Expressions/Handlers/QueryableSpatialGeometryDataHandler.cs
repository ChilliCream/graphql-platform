using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Spatial.Filters
{
    public class QueryableSpatialGeometryDataHandler
        : QueryableDataOperationHandler
    {
        protected override int Operation => SpatialFilterOperations.Geometry;
    }
}
