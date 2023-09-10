using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialNotIntersectsOperationHandler
    : QueryableSpatialIntersectsOperationHandlerBase
{
    public QueryableSpatialNotIntersectsOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.NotIntersects;

    protected override bool IsTrue => false;
}
