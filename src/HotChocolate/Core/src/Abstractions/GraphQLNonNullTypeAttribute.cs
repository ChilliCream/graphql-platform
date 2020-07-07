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
        public bool[]? Nullable { get; set; }
    }
}
