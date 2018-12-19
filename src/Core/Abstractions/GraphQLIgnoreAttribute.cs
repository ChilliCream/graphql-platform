using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Property
        | AttributeTargets.Method)]
    public sealed class GraphQLIgnoreAttribute
        : Attribute
    {
    }
}
