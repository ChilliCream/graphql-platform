using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialNotTouchesOperationHandler
        : QueryableSpatialTouchesOperationHandlerBase
    {
        public QueryableSpatialNotTouchesOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.NotTouches;

        protected override bool IsTrue => false;
    }
}
