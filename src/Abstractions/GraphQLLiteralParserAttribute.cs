using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GraphQLLiteralParserAttribute
        : Attribute
    {
        public GraphQLLiteralParserAttribute(Type type)
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
