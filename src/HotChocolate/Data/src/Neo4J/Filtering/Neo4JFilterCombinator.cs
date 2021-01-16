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

            // TODO: Filter combination implementation
            combined = combinator switch
            {
                FilterCombinator.And => new(),
                FilterCombinator.Or => new(),
                _ => throw new InvalidOperationException()
            };

            return true;
        }
    }
}
