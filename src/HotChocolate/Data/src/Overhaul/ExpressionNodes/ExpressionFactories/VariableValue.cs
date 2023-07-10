using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class VariableValue : IExpressionFactory
{
    [Dependency(Expression = true)]
    public Identifier ExpressionId { get; }

    public VariableValue(Identifier expressionId)
    {
        ExpressionId = expressionId;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var expressions = context.Variables.Expressions[ExpressionId];
        return expressions.ValueExpression;
    }
}
