using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GraphQLLiteralParserAttribute
        : Attribute
    {
        public GraphQLLiteralParserAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Type Type { get; }
    }
}
