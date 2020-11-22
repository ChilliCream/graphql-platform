using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialNotWithinOperationHandler
        : QueryableSpatialWithinOperationHandlerBase
    {
        public QueryableSpatialNotWithinOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.NotWithin;

        protected override bool IsTrue => false;
    }
}
