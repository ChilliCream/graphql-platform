using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

static file class Cache
{
    private static readonly MethodInfo ContainsMethod = typeof(Enumerable)
        .GetMethods()
        .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

    // private static ThreadLocal<Type[]> _typeArray = new(() => new Type[1]);

    public static MethodInfo GetContainsMethod(Type type)
    {
        // var tempArray = _typeArray.Value!;
        // tempArray[0] = type;
        // return ContainsMethod.MakeGenericMethod(tempArray);
        return ContainsMethod.MakeGenericMethod(type);
    }
}

// Can be used for both cases:
// `in`: Variable([1, 2, 3]).Contains(x.Value)
// `contains`: x.Array.Contains(Variable(1))
[NoStructuralDependencies]
public sealed class Contains : IPredicateExpressionFactory
{
    private readonly MethodInfo _containsMethod;

    public Contains(Type valueType)
    {
        _containsMethod = Cache.GetContainsMethod(valueType);
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        var array = children[0];
        var value = children[1];
        return Expression.Call(_containsMethod, array, value);
    }
}
