using System;

namespace HotChocolate
{
    [AttributeUsage(
        AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Parameter)]
    public sealed class GraphQLNonNullTypeAttribute
        : Attribute
    {
        public bool IsElementNullable { get; set; }

        public bool IsNullable { get; set; }
    }
}
