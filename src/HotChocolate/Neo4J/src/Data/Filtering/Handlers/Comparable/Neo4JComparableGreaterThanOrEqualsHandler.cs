using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a GreaterThanOrEquals operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JComparableGreaterThanOrEqualsHandler
        : Neo4JComparableOperationHandler
    {
        public Neo4JComparableGreaterThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.GreaterThanOrEquals;

        /// <inheritdoc />
        public override Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is null) throw new InvalidOperationException();

            Condition? expression = context
                .GetNode()
                .Property(context.GetNeo4JFilterScope().GetPath()).GreaterThanOEqualTo(Cypher.LiteralOf(parsedValue));

            return expression;
        }
    }
}
