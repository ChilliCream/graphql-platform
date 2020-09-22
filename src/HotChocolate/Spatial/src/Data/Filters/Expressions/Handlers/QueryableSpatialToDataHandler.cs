using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Spatial.Filters
{
    public class QueryableSpatialToDataHandler
        : QueryableDataOperationHandler
    {
        protected override int Operation => SpatialFilterOperations.To;
    }
}
