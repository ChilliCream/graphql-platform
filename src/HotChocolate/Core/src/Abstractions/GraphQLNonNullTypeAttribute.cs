using System;

#nullable enable

namespace HotChocolate
{
    [AttributeUsage(
        AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Parameter)]
    public sealed class GraphQLNonNullTypeAttribute
        : Attribute
    {
        public GraphQLNonNullTypeAttribute()
        {
            Nullable =  new bool[] { false };
        }

        public GraphQLNonNullTypeAttribute(params bool[] nullable)
        {
            Nullable = nullable.Length == 0
                ? new bool[] { false }
                : nullable;
        }

        public bool[] Nullable { get; } = new bool[] { false };
    }
}
