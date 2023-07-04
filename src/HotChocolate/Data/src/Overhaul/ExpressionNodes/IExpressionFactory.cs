using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public interface IExpressionFactory
{
    Expression GetExpression(IExpressionCompilationContext context);
}
