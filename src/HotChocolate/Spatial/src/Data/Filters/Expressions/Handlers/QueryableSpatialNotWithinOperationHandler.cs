using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialNotWithinOperationHandler
    : QueryableSpatialWithinOperationHandlerBase
{
    public QueryableSpatialNotWithinOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.NotWithin;

    protected override bool IsTrue => false;
}
