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
            Nullable =  new[] { false };
        }

        public GraphQLNonNullTypeAttribute(params bool[] nullable)
        {
            Nullable = nullable.Length == 0
                ? new[] { false }
                : nullable;
        }

        public bool[] Nullable { get; }
    }
}
