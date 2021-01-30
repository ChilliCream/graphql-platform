using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JStringEndsWithHandler
        : Neo4JStringOperationHandler
    {
        public Neo4JStringEndsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.EndsWith;

        public override Neo4JFilterDefinition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is not string str) throw new InvalidOperationException();
            var doc = new Neo4JFilterOperation(
                Operator.EndsWith.GetRepresentation(),
                str);

            return new Neo4JFilterOperation(context.GetNeo4JFilterScope().GetPath(), doc);
        }
    }
}
