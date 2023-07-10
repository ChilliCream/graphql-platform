using System;
using System.Reflection;
using System.Threading;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class GenericMethod2Cache
{
    private static ThreadLocal<Type[]> _typeArray = new(() => new Type[1]);
    private readonly MethodInfo _method;

    public GenericMethod2Cache(MethodInfo method)
    {
        _method = method;
    }

    public MethodInfo GetMethod(Type type1, Type type2)
    {
        var tempArray = _typeArray.Value!;
        tempArray[0] = type1;
        tempArray[1] = type2;
        return _method.MakeGenericMethod(type1, type2);
    }
}
