using System.Collections.Generic;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
/// <summary>
    /// This filter operation handler maps a All operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JListAllOperationHandler : Neo4JListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.All;

        /// <inheritdoc />
        protected override Neo4JFilterDefinition HandleListOperation(
            Neo4JFilterVisitorContext context,
            IFilterField field,
            Neo4JFilterScope scope,
            string path) =>
            field.Type is IComparableOperationFilterInputType
                ? CreateArrayAllScalar(scope, path)
                : CreateArrayAll(scope, path);

        private static Neo4JFilterDefinition CreateArrayAll(
            Neo4JFilterScope scope,
            string path)
        {
            var negatedChilds = new List<Neo4JFilterDefinition>();
            Queue<Neo4JFilterDefinition> level = scope.Level.Peek();
            while (level.Count > 0)
            {
                negatedChilds.Add(level.Dequeue());
            }

            return new Neo4JFilterOperation(
                path,
                new Neo4JNotFilterDefinition(
                    new Neo4JFilterOperation(
                        "$elemMatch",
                        new Neo4JNotFilterDefinition(
                            new Neo4JOrFilterDefinition(negatedChilds)))));
        }

        private static Neo4JFilterDefinition CreateArrayAllScalar(
            Neo4JFilterScope scope,
            string path)
        {
            var negatedChilds = new List<Neo4JFilterDefinition>();
            Queue<Neo4JFilterDefinition> level = scope.Level.Peek();
            while (level.Count > 0)
            {
                negatedChilds.Add(
                    new Neo4JFilterOperation(
                        path,
                        new Neo4JNotFilterDefinition(level.Dequeue())));
            }

            return new Neo4JNotFilterDefinition(
                new Neo4JOrFilterDefinition(negatedChilds));
        }
    }
}
