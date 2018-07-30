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
            IServiceProvider services,
            ISchemaContext context,
            IReadOnlySchemaOptions options)
        {
            Services = services;
            _types = SchemaTypes.Create(
                context.Types.GetTypes(),
                context.Types.GetTypeBindings(),
                options);
            Directives = context.Directives.GetDirectives();
            Options = options;
            Sessions = new SessionManager(
                services,
                context.DataLoaders,
                context.CustomContexts);
        }

        /// <summary>
        /// Gets the schema options.
        /// </summary>
        public IReadOnlySchemaOptions Options { get; }

        /// <summary>
        /// Gets the global schema services.
        /// </summary>
        public IServiceProvider Services { get; }

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

        /// <summary>
        /// Gets all the schema types.
        /// </summary>
        public IReadOnlyCollection<INamedType> Types => _types.GetTypes();

        /// <summary>
        /// Gets all the direcives that are supported by this schema.
        /// </summary>
        public IReadOnlyCollection<Directive> Directives { get; }

        /// <summary>
        /// Gets the session manager which can be used to create
        /// new query execution sessions.
        /// </summary>
        public ISessionManager Sessions { get; }

        /// <summary>
        /// Gets a type by its name and kind.
        /// </summary>
        /// <typeparam name="T">The expected type kind.</typeparam>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>The type.</returns>
        /// <exception cref="ArgumentException">
        /// The specified type does not exist or
        /// is not of the specified type kind.
        /// </exception>
        public T GetType<T>(string typeName)
            where T : INamedType
        {
            return _types.GetType<T>(typeName);
        }

        /// <summary>
        /// Tries to get a type by its name and kind.
        /// </summary>
        /// <typeparam name="T">The expected type kind.</typeparam>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="type">The resolved type.</param>
        /// <returns>
        /// <c>true</c>, if a type with the name exists and is of the specified
        /// kind, <c>false</c> otherwise.
        /// </returns>
        public bool TryGetType<T>(string typeName, out T type)
            where T : INamedType
        {
            return _types.TryGetType<T>(typeName, out type);
        }

        /// <summary>
        /// Tries to get the .net type representation of a schema.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="nativeType">The resolved .net type.</param>
        /// <returns>
        /// <c>true</c>, if a .net type was found that was bound
        /// the the specified schema type, <c>false</c> otherwise.
        /// </returns>
        public bool TryGetNativeType(string typeName, out Type nativeType)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            return _types.TryGetNativeType(typeName, out nativeType);
        }

        /// <summary>
        /// Gets the possible object types to
        /// an abstract type (union type or interface type).
        /// </summary>
        /// <param name="abstractType">The abstract type.</param>
        /// <returns>
        /// Returns a collection with all possible object types
        /// for the given abstract type.
        /// </returns>
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
