using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialTouchesOperationHandler
    : QueryableSpatialTouchesOperationHandlerBase
{
    public QueryableSpatialTouchesOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.Touches;

    protected override bool IsTrue => true;
}
