using System;
using System.Reflection;
using System.Threading;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class GenericMethod1Cache
{
    private static ThreadLocal<Type[]> _typeArray = new(() => new Type[1]);
    private readonly MethodInfo _method;

    public GenericMethod1Cache(MethodInfo method)
    {
        _method = method;
    }

    public MethodInfo GetMethod(Type type)
    {
        var arr = _typeArray.Value!;
        arr[0] = type;
        return _method.MakeGenericMethod(arr);
    }
}
