using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class
        | AttributeTargets.Property
        | AttributeTargets.Method)]
    public class GraphQLDescription
        : Attribute
    {
        public GraphQLDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }

            Name = description;
        }

        public string Name { get; }
    }
}
