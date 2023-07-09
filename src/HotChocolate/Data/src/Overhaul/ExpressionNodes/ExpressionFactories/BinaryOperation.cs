using System;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class BinaryOperation : ILogicalExpressionFactory
{
    public static readonly BinaryOperation Equal = new(Expression.Equal);
    public static readonly BinaryOperation NotEqual = new(Expression.NotEqual);

    public static readonly BinaryOperation GreaterThan = new(Expression.GreaterThan);
    public static readonly BinaryOperation GreaterThanOrEqual = new(Expression.GreaterThanOrEqual);
    public static readonly BinaryOperation LessThan = new(Expression.LessThan);
    public static readonly BinaryOperation LessThanOrEqual = new(Expression.LessThanOrEqual);

    public static BinaryOperation NotGreaterThan => LessThanOrEqual;
    public static BinaryOperation NotGreaterThanOrEqual => LessThan;
    public static BinaryOperation NotLessThan => GreaterThanOrEqual;
    public static BinaryOperation NotLessThanOrEqual => GreaterThan;

    private readonly Func<Expression, Expression, BinaryExpression> _operation;

    public BinaryOperation(Func<Expression, Expression, BinaryExpression> operation)
    {
        _operation = operation;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        var lhs = children[0];
        var rhs = children[1];
        return _operation(lhs, rhs);
    }
}
