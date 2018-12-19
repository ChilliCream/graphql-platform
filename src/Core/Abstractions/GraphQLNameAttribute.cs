using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class
        | AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Parameter)]
    public sealed class GraphQLNameAttribute
        : Attribute
    {
        public GraphQLNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public string Name { get; }
    }
}
