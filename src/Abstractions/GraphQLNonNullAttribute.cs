using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Property
        | AttributeTargets.Method)]
    public sealed class GraphQLNonNullAttribute
        : Attribute
    {
        public bool ElementIsNullable { get; set; }

        public bool IsNullable { get; set; }
    }
}
