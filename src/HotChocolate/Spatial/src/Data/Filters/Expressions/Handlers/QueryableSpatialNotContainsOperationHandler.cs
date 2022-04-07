using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialNotContainsOperationHandler
    : QueryableSpatialContainsOperationHandlerBase
{
    public QueryableSpatialNotContainsOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.NotContains;

    protected override bool IsTrue => false;
}
