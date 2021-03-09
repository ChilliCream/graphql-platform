using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a Some operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JListSomeOperationHandler// : Neo4JListOperationHandlerBase
    {
        /*/// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.Some;

        protected override Neo4JFilterDefinition HandleListOperation(Neo4JFilterVisitorContext context, IFilterField field, Neo4JFilterScope scope,
            string path)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        protected override Neo4JFilterDefinition HandleListOperation(
            Neo4JFilterVisitorContext context,
            IFilterField field,
            Neo4JFilterScope scope,
            string path)
        {
            return new Neo4JFilterOperation(
                path,
                new Neo4JFilterOperation("$elemMatch", CombineOperationsOfScope(scope)));
        }*/
    }
}
