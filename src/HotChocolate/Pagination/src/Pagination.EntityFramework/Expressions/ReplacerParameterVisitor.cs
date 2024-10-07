using System.Linq.Expressions;

namespace HotChocolate.Pagination.Expressions;

internal sealed class ReplacerParameterVisitor(
    ParameterExpression oldParameter,
    ParameterExpression newParameter)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == oldParameter
            ? newParameter
            : base.VisitParameter(node);
}
