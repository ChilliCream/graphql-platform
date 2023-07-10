using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class CallStaticMethod2 : ILogicalExpressionFactory
{
    private readonly MethodInfo _method;

    public CallStaticMethod2(MethodInfo method)
    {
        Debug.Assert(method.GetParameters().Length == 2);
        _method = method;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        var array = children[0];
        var value = children[1];
        // Could also retrieve the type here and use the method cache instead?
        return Expression.Call(_method, array, value);
    }
}
