using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialWithinOperationHandler
        : QueryableSpatialWithinOperationHandlerBase
    {
        public QueryableSpatialWithinOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.Within;

        protected override bool IsTrue => true;
    }
}
