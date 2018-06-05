using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
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
    {
        private readonly SchemaTypes _types;
        private readonly IntrospectionFields _introspectionFields;

        private Schema(
            IServiceProvider services,
            SchemaTypes types,
            IntrospectionFields introspectionFields)
        {
            _types = types;
            _introspectionFields = introspectionFields;
            Services = services;
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

        internal __SchemaField SchemaField => _introspectionFields.SchemaField;

        internal __TypeField TypeField => _introspectionFields.TypeField;

        internal __TypeNameField TypeNameField => _introspectionFields.TypeNameField;

        internal IServiceProvider Services { get; }

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

        public IReadOnlyCollection<INamedType> GetAllTypes()
        {
            return _types.GetTypes();
        }

        // TODO : Introduce directive type
        internal IReadOnlyCollection<object> GetDirectives()
        {
            return Array.Empty<object>();
        }

        internal IReadOnlyCollection<ObjectType> GetPossibleTypes(
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

        internal bool TryGetNativeType(string typeName, out Type nativeType)
        {
            return _types.TryGetNativeType(typeName, out nativeType);
        }
    }
}
