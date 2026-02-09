using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Projections.Spatial;

public class QueryableSpatialProjectionScalarHandler
    : QueryableProjectionScalarHandler
{
    public override bool CanHandle(Selection selection) =>
        selection.Field.Member is not null
        && typeof(Geometry).IsAssignableFrom(selection.Field.Member.GetReturnType());

    public static new QueryableSpatialProjectionScalarHandler Create(ProjectionProviderContext context) => new();
}
