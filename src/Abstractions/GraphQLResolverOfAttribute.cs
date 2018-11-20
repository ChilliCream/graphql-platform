using System;
using System.Collections.Generic;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class GraphQLResolverOfAttribute
        : Attribute
    {
        public GraphQLResolverOfAttribute(params Type[] types)
        {
            Types = types
                ?? throw new ArgumentNullException(nameof(types));
        }

        public GraphQLResolverOfAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Types = new[] { type };
        }

        public IReadOnlyCollection<Type> Types { get; }
    }
}
