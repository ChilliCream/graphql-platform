using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a All operation field.
    /// </summary>
    public class Neo4JListAllOperationHandler : Neo4JListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.All;

        /// <inheritdoc />
        protected override Condition HandleListOperation(
            Neo4JFilterVisitorContext context,
            IFilterField field,
            Neo4JFilterScope scope,
            string path) =>
            field.Type is IComparableOperationFilterInputType
                ? CreateArrayAllScalar(scope, path)
                : CreateArrayAll(scope, path);

        private static Condition CreateArrayAll(
            Neo4JFilterScope scope,
            string path)
        {

            var compound = new CompoundCondition(Operator.And);
            return compound;
        }

        private static Condition CreateArrayAllScalar(
            Neo4JFilterScope scope,
            string path)
        {

            return new CompoundCondition(Operator.And);
        }
    }
}
