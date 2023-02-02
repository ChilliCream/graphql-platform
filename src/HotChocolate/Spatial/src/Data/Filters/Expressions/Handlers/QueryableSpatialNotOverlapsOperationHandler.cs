using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialNotOverlapsOperationHandler
    : QueryableSpatialOverlapsOperationHandlerBase
{
    public QueryableSpatialNotOverlapsOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.NotOverlaps;

    protected override bool IsTrue => false;
}
