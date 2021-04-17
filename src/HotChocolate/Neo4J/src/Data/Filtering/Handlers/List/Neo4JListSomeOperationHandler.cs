using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JListSomeOperationHandler : Neo4JListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.Some;

        /// <inheritdoc />
        protected override Condition HandleListOperation(
            Neo4JFilterVisitorContext context,
            IFilterField field,
            Neo4JFilterScope scope,
            string path)
        {
            return new CompoundCondition(Operator.And);
        }
    }
}
