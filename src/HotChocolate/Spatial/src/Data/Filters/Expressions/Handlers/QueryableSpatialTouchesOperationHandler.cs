using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialTouchesOperationHandler
        : QueryableSpatialTouchesOperationHandlerBase
    {
        public QueryableSpatialTouchesOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.Touches;

        protected override bool IsTrue => true;
    }
}
