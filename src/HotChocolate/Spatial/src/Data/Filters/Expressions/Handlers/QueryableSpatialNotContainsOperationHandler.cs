using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialNotContainsOperationHandler
        : QueryableSpatialContainsOperationHandlerBase
    {
        public QueryableSpatialNotContainsOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.NotContains;

        protected override bool IsTrue => false;
    }
}
