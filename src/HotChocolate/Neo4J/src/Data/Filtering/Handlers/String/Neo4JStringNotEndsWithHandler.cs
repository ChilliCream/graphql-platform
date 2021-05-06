#nullable enable
using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JStringNotEndsWithHandler
        : Neo4JStringOperationHandler
    {
        public Neo4JStringNotEndsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotEndsWith;

        public override Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is not string str) throw new InvalidOperationException();

            Condition? expression = context
                .GetNode()
                .Property(context.GetNeo4JFilterScope().GetPath()).EndsWith(Cypher.LiteralOf(str))
                .Not();
            return expression;
        }
    }
}
