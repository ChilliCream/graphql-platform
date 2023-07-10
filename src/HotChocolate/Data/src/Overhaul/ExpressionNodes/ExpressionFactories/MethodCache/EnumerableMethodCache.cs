using System.Linq;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

public static class EnumerableMethodCache
{
    private static MethodInfo GetEnumerableMethod(string name, int numParams)
    {
        return typeof(Enumerable)
            .GetMethods()
            .Single(m => m.Name == name && m.GetParameters().Length == numParams);
    }

    public static readonly GenericMethod1Cache Contains
        = new(GetEnumerableMethod(nameof(Enumerable.Contains), 2));

    public static readonly GenericMethod1Cache Where2
        = new(GetEnumerableMethod(nameof(Enumerable.Where), 2));

    public static readonly GenericMethod1Cache Any1
        = new(GetEnumerableMethod(nameof(Enumerable.Any), 1));

    public static readonly GenericMethod2Cache Select2
        = new(GetEnumerableMethod(nameof(Enumerable.Select), 2));

    public static readonly GenericMethod1Cache First1
        = new(GetEnumerableMethod(nameof(Enumerable.First), 1));
}
