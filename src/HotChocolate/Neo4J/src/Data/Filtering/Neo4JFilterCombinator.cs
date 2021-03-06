using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JFilterCombinator
        : FilterOperationCombinator<Neo4JFilterVisitorContext, Neo4JFilterDefinition>
    {
        /// <inheritdoc />
        public override bool TryCombineOperations(
            Neo4JFilterVisitorContext context,
            Queue<Neo4JFilterDefinition> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out Neo4JFilterDefinition combined)
        {
            if (operations.Count < 1)
            {
                throw new InvalidOperationException();
            }

            combined = combinator switch
            {
                FilterCombinator.And => CombineWithAnd(context, operations),
                FilterCombinator.Or => CombineWithOr(context, operations),
                _ => throw new InvalidOperationException()
            };

            return true;
        }

        private static Neo4JFilterDefinition CombineWithAnd(
            Neo4JFilterVisitorContext context,
            Queue<Neo4JFilterDefinition> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return new Neo4JAndFilterDefinition(operations.ToArray());
        }

        private static Neo4JFilterDefinition CombineWithOr(
            Neo4JFilterVisitorContext context,
            Queue<Neo4JFilterDefinition> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return new Neo4JOrFilterDefinition(operations.ToArray());
        }
    }
}
