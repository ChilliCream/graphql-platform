using System;

namespace HotChocolate.Data.ExpressionNodes;

public static class ExpressionFactories
{
    // Can be used for both cases:
    // `in`: Variable([1, 2, 3]).Contains(x.Value)
    // `contains`: x.Array.Contains(Variable(1))
    public static CallStaticMethod2 Contains(Type valueType)
        => new(EnumerableMethodCache.Contains.GetMethod(valueType));

    public static readonly CallInstanceMethod1 StartsWith = new(StringMethodCache.StartsWith);
    public static readonly CallInstanceMethod1 EndsWith = new(StringMethodCache.EndsWith);
}
