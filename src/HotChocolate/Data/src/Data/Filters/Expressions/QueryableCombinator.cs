using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableCombinator
    : FilterOperationCombinator<QueryableFilterContext, Expression>
{
    public override bool TryCombineOperations(
        QueryableFilterContext context,
        Queue<Expression> operations,
        FilterCombinator combinator,
        [NotNullWhen(true)] out Expression? combined)
    {
        if (operations.Count == 0)
        {
            combined = null;
            return false;
        }

        combined = CombineOperations(operations, combinator);

        return true;
    }

    private Expression CombineOperations(
        Queue<Expression> operations,
        FilterCombinator combinator)
    {
        while (operations.Count > 1)
        {
            var operationCount = operations.Count;
            var pairCount = operationCount / 2;

            for (var i = 0; i < pairCount; i++)
            {
                var left = operations.Dequeue();
                var right = operations.Dequeue();

                operations.Enqueue(Combine(left, right, combinator));
            }

            if ((operationCount & 1) == 1)
            {
                operations.Enqueue(operations.Dequeue());
            }
        }

        return operations.Dequeue();
    }

    private BinaryExpression Combine(
        Expression left,
        Expression right,
        FilterCombinator combinator)
        => combinator switch
        {
            FilterCombinator.And => Expression.AndAlso(left, right),
            FilterCombinator.Or => Expression.OrElse(left, right),
            _ => throw ThrowHelper.Filtering_QueryableCombinator_InvalidCombinator(this, combinator)
        };
}
