using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialBufferDataHandler
    : QueryableDataOperationHandler
{
    protected override int Operation => SpatialFilterOperations.Buffer;

    public static new QueryableSpatialBufferDataHandler Create(FilterProviderContext context) => new();
}
