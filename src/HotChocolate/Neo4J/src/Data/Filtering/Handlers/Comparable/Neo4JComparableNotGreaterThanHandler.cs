using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a NotGreaterThan operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JComparableNotGreaterThanHandler
        : Neo4JComparableOperationHandler
    {
        public Neo4JComparableNotGreaterThanHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.NotGreaterThan;

        /// <inheritdoc />
        public override Neo4JFilterDefinition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new Neo4JNotFilterDefinition(
                    new Neo4JFilterOperation("$gt", parsedValue));

                return new Neo4JFilterOperation(context.GetNeo4JFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
