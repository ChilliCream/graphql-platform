using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Property
        | AttributeTargets.Method)]
    public sealed class GraphQLTypeAttribute
        : Attribute
    {
        public GraphQLTypeAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public Type Type { get; }
    }
}
