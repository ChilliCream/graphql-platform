using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialOverlapsOperationHandler
    : QueryableSpatialOverlapsOperationHandlerBase
{
    public QueryableSpatialOverlapsOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.Overlaps;

    protected override bool IsTrue => true;
}
