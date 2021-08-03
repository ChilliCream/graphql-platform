using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <inheritdoc />
    public class SqlKataFilterCombinator
        : FilterOperationCombinator<SqlKataFilterVisitorContext, Query>
    {
        /// <inheritdoc />
        public override bool TryCombineOperations(
            SqlKataFilterVisitorContext context,
            Queue<Query> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out Query combined)
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

        private static Query CombineWithAnd(
            SqlKataFilterVisitorContext context,
            Queue<Query> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            var query = new Query();
            query.Clauses.AddRange(operations.SelectMany(x => x.Clauses));
            return query;
        }

        private static Query CombineWithOr(
            SqlKataFilterVisitorContext context,
            Queue<Query> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            var query = new Query();
            query.Clauses.AddRange(operations.SelectMany(x => x.Clauses.Select(y =>
            {
                if (y is AbstractCondition condition)
                {
                    condition.IsOr = !condition.IsOr;
                }
                return y;

            })));
            return query;
        }
    }
}
