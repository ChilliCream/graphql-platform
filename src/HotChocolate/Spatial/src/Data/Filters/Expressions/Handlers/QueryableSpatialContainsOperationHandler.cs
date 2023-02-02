using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial;

public class QueryableSpatialContainsOperationHandler
    : QueryableSpatialContainsOperationHandlerBase
{
    public QueryableSpatialContainsOperationHandler(
        IFilterConvention convention,
        ITypeInspector inspector,
        InputParser inputParser)
        : base(convention, inspector, inputParser)
    {
    }

    protected override int Operation => SpatialFilterOperations.Contains;

    protected override bool IsTrue => true;
}
