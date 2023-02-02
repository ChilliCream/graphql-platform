using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialWithinOperationHandler
    : QueryableSpatialWithinOperationHandlerBase
{
    public QueryableSpatialWithinOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.Within;

    protected override bool IsTrue => true;
}
