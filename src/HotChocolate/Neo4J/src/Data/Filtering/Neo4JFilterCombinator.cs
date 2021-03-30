using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JFilterCombinator
        : FilterOperationCombinator<Neo4JFilterVisitorContext, Condition>
    {
        /// <inheritdoc />
        public override bool TryCombineOperations(
            Neo4JFilterVisitorContext context,
            Queue<Condition> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out Condition combined)
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

        private static CompoundCondition CombineWithAnd(
            Neo4JFilterVisitorContext context,
            IReadOnlyCollection<Condition> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            var conditions = new CompoundCondition(Operator.And);
            foreach (Condition condition in operations)
            {
                conditions.And(condition);
            }

            return conditions;
        }

        private static Condition CombineWithOr(
            Neo4JFilterVisitorContext context,
            Queue<Condition> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            var conditions = new CompoundCondition(Operator.Or);
            foreach (Condition condition in operations)
            {
                conditions.Or(condition);
            }

            return conditions;
        }
    }
}
