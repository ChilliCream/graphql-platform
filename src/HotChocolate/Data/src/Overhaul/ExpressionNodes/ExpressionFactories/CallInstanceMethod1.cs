using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class CallInstanceMethod1 : ILogicalExpressionFactory
{
    // The overload that takes plain `arg0` instead of params array is internal.
    private static ThreadLocal<Expression[]> _parametersCache = new(() => new Expression[1]);

    private readonly MethodInfo _method;

    public CallInstanceMethod1(MethodInfo method)
    {
        Debug.Assert(method.GetParameters().Length == 1);
        _method = method;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        var instance = children[0];
        var value = children[1];

        var parameters = _parametersCache.Value!;
        parameters[0] = value;
        return Expression.Call(instance, _method, parameters);
    }
}
