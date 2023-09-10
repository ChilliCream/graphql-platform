using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialNotTouchesOperationHandler
    : QueryableSpatialTouchesOperationHandlerBase
{
    public QueryableSpatialNotTouchesOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.NotTouches;

    protected override bool IsTrue => false;
}
