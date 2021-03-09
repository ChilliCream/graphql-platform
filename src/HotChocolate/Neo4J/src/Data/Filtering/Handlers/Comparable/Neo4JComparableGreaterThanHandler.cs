using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a GreaterThan operation field to a
    /// <see cref="Condition"/>
    /// </summary>
    public class Neo4JComparableGreaterThanHandler
        : Neo4JComparableOperationHandler
    {
        public Neo4JComparableGreaterThanHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.GreaterThan;

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
                .Property(context.GetNeo4JFilterScope().GetPath()).GreaterThan(Cypher.LiteralOf(parsedValue));

            return expression;
        }
    }
}
