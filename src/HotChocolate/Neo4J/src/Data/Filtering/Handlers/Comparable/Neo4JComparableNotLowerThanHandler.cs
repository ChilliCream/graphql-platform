using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a NotLowerThan operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JComparableNotLowerThanHandler
        : Neo4JComparableOperationHandler
    {
        public Neo4JComparableNotLowerThanHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.NotLowerThan;

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
                    new Neo4JFilterOperation(Operator.LessThan.GetRepresentation(), parsedValue));

                return new Neo4JFilterOperation(context.GetNeo4JFilterScope().GetPath(), doc);
            }

            throw new InvalidOperationException();
        }
    }
}
