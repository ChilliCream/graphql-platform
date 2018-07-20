using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Runtime;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate
{
    /// <summary>
    /// A GraphQL Schema defines the capabilities of a GraphQL server. It
    /// exposes all available types and directives on the server, as well as
    /// the entry points for query, mutation, and subscription operations.
    /// </summary>
    public partial class Schema
        : ISchema
    {
        private readonly SchemaTypes _types;

        private Schema(
            SchemaTypes types,
            IReadOnlyCollection<Directive> directives,
            IReadOnlySchemaOptions options)
        {
            _types = types;
            Options = options;
            Directives = directives;
        }

        /// <summary>
        /// The type that query operations will be rooted at.
        /// </summary>
        public ObjectType QueryType => _types.QueryType;

        /// <summary>
        /// If this server supports mutation, the type that
        /// mutation operations will be rooted at.
        /// </summary>
        public ObjectType MutationType => _types.MutationType;

        /// <summary>
        /// If this server support subscription, the type that
        /// subscription operations will be rooted at.
        /// </summary>
        public ObjectType SubscriptionType => _types.SubscriptionType;

        public IReadOnlySchemaOptions Options { get; }

        public IReadOnlyCollection<INamedType> Types => _types.GetTypes();

        public IReadOnlyCollection<Directive> Directives { get; }

        public IReadOnlyCollection<DataLoaderDescriptor> DataLoaders { get; }

        public IReadOnlyCollection<StateObjectDescriptor> StateObjects { get; }

        public T GetType<T>(string typeName)
            where T : INamedType
        {
            return _types.GetType<T>(typeName);
        }

        public bool TryGetType<T>(string typeName, out T type)
            where T : INamedType
        {
            return _types.TryGetType<T>(typeName, out type);
        }

        public IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType)
        {
            if (abstractType == null)
            {
                throw new ArgumentNullException(nameof(abstractType));
            }

            if (_types.TryGetPossibleTypes(
                abstractType.Name,
                out ImmutableList<ObjectType> types))
            {
                return types;
            }

            return Array.Empty<ObjectType>();
        }

        public bool TryGetNativeType(string typeName, out Type nativeType)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            return _types.TryGetNativeType(typeName, out nativeType);
        }
    }
}
