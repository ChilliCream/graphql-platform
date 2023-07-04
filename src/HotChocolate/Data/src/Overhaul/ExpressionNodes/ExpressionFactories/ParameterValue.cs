using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class ParameterValue : IExpressionFactory
{
    public Identifier ParameterId { get; }

    public ParameterValue(Identifier parameterId)
    {
        ParameterId = parameterId;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var parameter = context.Parameters.Expressions[ParameterId];
        return parameter.ValueExpression;
    }
}
