using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Projections.Spatial
{
    public class QueryableSpatialProjectionScalarHandler
        : QueryableProjectionScalarHandler
    {
        public override bool CanHandle(ISelection selection) =>
            selection.Field.Member is {} &&
            typeof(Geometry).IsAssignableFrom(selection.Field.Member.GetReturnType());
    }
}
