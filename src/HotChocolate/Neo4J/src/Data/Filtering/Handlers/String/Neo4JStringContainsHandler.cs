#nullable enable
using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JStringContainsHandler
        : Neo4JStringOperationHandler
    {
        public Neo4JStringContainsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.Contains;

        public override Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is not string str) throw new InvalidOperationException();

            Condition? expression = context
                .GetNode()
                .Property(context.GetNeo4JFilterScope().GetPath()).Contains(Cypher.LiteralOf(str));
            return expression;
        }
    }
}
