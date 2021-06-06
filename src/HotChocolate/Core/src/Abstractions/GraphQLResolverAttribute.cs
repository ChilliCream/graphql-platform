﻿using System;
using System.Collections.Generic;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [Obsolete("Use ExtendObjectTypeAttribute.")]
    public sealed class GraphQLResolverAttribute
        : Attribute
    {
        public GraphQLResolverAttribute(params Type[] resolverTypes)
        {
            ResolverTypes = resolverTypes
                ?? throw new ArgumentNullException(nameof(resolverTypes));
        }

        public GraphQLResolverAttribute(Type resolverType)
        {
            if (resolverType is null)
            {
                throw new ArgumentNullException(nameof(resolverType));
            }

            ResolverTypes = new[] { resolverType };
        }

        public IReadOnlyCollection<Type> ResolverTypes { get; }
    }
}
