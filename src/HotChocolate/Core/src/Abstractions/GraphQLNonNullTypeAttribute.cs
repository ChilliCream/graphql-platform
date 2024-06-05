using System;

namespace HotChocolate;

[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Parameter)]
public sealed class GraphQLNonNullTypeAttribute : Attribute
{
    public GraphQLNonNullTypeAttribute()
    {
        Nullable = [false,];
    }

    public GraphQLNonNullTypeAttribute(params bool[] nullable)
    {
        Nullable = nullable.Length == 0 ? [false,] : nullable;
    }

    internal bool[] Nullable { get; }
}
