using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class InstanceExpressionFactory : IExpressionFactory
{
    public static readonly InstanceExpressionFactory Instance = new();
    
    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var name = "x" + context.NodeId.Value;
        return Expression.Variable(context.ExpectedExpressionType, name);
    }
}
