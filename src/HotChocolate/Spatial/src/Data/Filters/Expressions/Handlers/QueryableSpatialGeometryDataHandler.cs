using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialGeometryDataHandler : QueryableDataOperationHandler
{
    protected override int Operation => SpatialFilterOperations.Geometry;

    public static new QueryableSpatialGeometryDataHandler Create(FilterProviderContext context) => new();
}
