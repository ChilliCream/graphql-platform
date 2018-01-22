using System;

namespace Zeus
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class GraphQLNameAttribute
        : Attribute
    {
        public GraphQLNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
}