using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialNotIntersectsOperationHandler
        : QueryableSpatialIntersectsOperationHandlerBase
    {
        public QueryableSpatialNotIntersectsOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector)
        {
        }

        protected override int Operation => SpatialFilterOperations.NotIntersects;

        protected override bool IsTrue => false;
    }
}
