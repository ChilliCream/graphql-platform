using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Spatial.Filters
{
    public class QueryableSpatialBufferDataHandler
        : QueryableDataOperationHandler
    {
        protected override int Operation => SpatialFilterOperations.Buffer;
    }
}
