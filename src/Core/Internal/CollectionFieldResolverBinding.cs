using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate
{
    internal class CollectionFieldResolverBinding
        : IFieldResolverBinding
    {
        public CollectionFieldResolverBinding(
            Type objectType,
            Type resolverCollection)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (resolverCollection == null)
            {
                throw new ArgumentNullException(nameof(resolverCollection));
            }

            ObjectType = objectType;
            ResolverCollection = resolverCollection;
            ExplicitBindings = new Dictionary<string, MemberInfo>();
        }

        public CollectionFieldResolverBinding(
            Type objectType,
            Type resolverCollection,
            Dictionary<string, MemberInfo> explicitBindings)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (resolverCollection == null)
            {
                throw new ArgumentNullException(nameof(resolverCollection));
            }

            if (explicitBindings == null)
            {
                throw new ArgumentNullException(nameof(explicitBindings));
            }

            ObjectType = objectType;
            ResolverCollection = resolverCollection;
            ExplicitBindings = explicitBindings;
        }

        public Type ObjectType { get; }
        public Type ResolverCollection { get; }
        public Dictionary<string, MemberInfo> ExplicitBindings { get; }
    }


}
