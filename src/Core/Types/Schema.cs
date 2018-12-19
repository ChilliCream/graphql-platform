using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Utilities;
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
        private bool _disposed;
        private readonly Dictionary<string, DirectiveType> _directiveTypes;

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
            _directiveTypes = context.Directives
                .GetDirectiveTypes()
                .ToDictionary(t => t.Name);
            DirectiveTypes = _directiveTypes.Values;
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
        public IReadOnlyCollection<DirectiveType> DirectiveTypes { get; }

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
        public T GetType<T>(NameString typeName)
            where T : INamedType
        {
            return _types.GetType<T>(typeName.EnsureNotEmpty(nameof(typeName)));
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
        public bool TryGetType<T>(NameString typeName, out T type)
            where T : INamedType
        {
            return _types.TryGetType<T>(
                typeName.EnsureNotEmpty(nameof(typeName)),
                out type);
        }

        /// <summary>
        /// Tries to get the .net type representation of a schema.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="clrType">The resolved .net type.</param>
        /// <returns>
        /// <c>true</c>, if a .net type was found that was bound
        /// the the specified schema type, <c>false</c> otherwise.
        /// </returns>
        public bool TryGetClrType(NameString typeName, out Type clrType)
        {
            return _types.TryGetClrType(
                typeName.EnsureNotEmpty(nameof(typeName)),
                out clrType);
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

        /// <summary>
        /// Gets a directive type by its name.
        /// </summary>
        /// <param name="directiveName">
        /// The directive name.
        /// </param>
        /// <returns>
        /// Returns directive type that was resolved by the given name
        /// or <c>null</c> if there is no directive with the specified name.
        /// </returns>
        public DirectiveType GetDirectiveType(NameString directiveName)
        {
            _directiveTypes.TryGetValue(
                directiveName.EnsureNotEmpty(nameof(directiveName)),
                out DirectiveType type);
            return type;
        }

        /// <summary>
        /// Tries to get a directive type by its name.
        /// </summary>
        /// <param name="directiveName">
        /// The directive name.
        /// </param>
        /// <param name="directiveType">
        /// The directive type that was resolved by the given name
        /// or <c>null</c> if there is no directive with the specified name.
        /// </param>
        /// <returns>
        /// <c>true</c>, if a directive type with the specified
        /// name exists; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetDirectiveType(
            NameString directiveName,
            out DirectiveType directiveType)
        {
            return _directiveTypes.TryGetValue(
                directiveName.EnsureNotEmpty(nameof(directiveName)),
                out directiveType);
        }

        public override string ToString()
        {
            return SchemaSerializer.Serialize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Sessions.Dispose();
                _disposed = true;
            }
        }
    }
}
