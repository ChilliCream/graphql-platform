using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a NotGreaterThanOrEquals operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JComparableNotGreaterThanOrEqualsHandler
        : Neo4JComparableOperationHandler
    {
        public Neo4JComparableNotGreaterThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.NotGreaterThanOrEquals;

        /// <inheritdoc />
        public override Neo4JFilterDefinition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is null) throw new InvalidOperationException();

            var doc = new Neo4JNotFilterDefinition(
                new Neo4JFilterOperation("$gte", parsedValue));

            return new Neo4JFilterOperation(context.GetNeo4JFilterScope().GetPath(), doc);
        }
    }
}
