using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal class ReplaceParameterVisitor(ParameterExpression parameter, Expression replacement)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == parameter ? replacement : base.VisitParameter(node);
    }
}
