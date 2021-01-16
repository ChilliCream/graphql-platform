using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialIntersectsOperationHandler
        : QueryableSpatialIntersectsOperationHandlerBase
    {
        public QueryableSpatialIntersectsOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.Intersects;

        protected override bool IsTrue => true;
    }
}
