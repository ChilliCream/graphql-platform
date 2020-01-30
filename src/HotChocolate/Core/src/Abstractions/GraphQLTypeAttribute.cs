using System;

namespace HotChocolate
{
    [AttributeUsage(
        AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Parameter)]
    public sealed class GraphQLTypeAttribute
        : Attribute
    {
        public GraphQLTypeAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Type Type { get; }
    }
}
