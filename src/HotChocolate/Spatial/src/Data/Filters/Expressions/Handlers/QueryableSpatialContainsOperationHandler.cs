using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialContainsOperationHandler
        : QueryableSpatialContainsOperationHandlerBase
    {
        public QueryableSpatialContainsOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.Contains;

        protected override bool IsTrue => true;
    }
}
