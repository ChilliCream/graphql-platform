﻿using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Runtime;
using HotChocolate.Types;

namespace HotChocolate
{
    /// <summary>
    /// A GraphQL Schema defines the capabilities of a GraphQL server. It
    /// exposes all available types and directives on the server, as well as
    /// the entry points for query, mutation, and subscription operations.
    /// </summary>
    public interface ISchema
        : IDisposable
    {
        /// <summary>
        /// Gets the schema options.
        /// </summary>
        IReadOnlySchemaOptions Options { get; }

        /// <summary>
        /// Gets the global schema services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// The type that query operations will be rooted at.
        /// </summary>
        ObjectType QueryType { get; }

        /// <summary>
        /// If this server supports mutation, the type that
        /// mutation operations will be rooted at.
        /// </summary>
        ObjectType MutationType { get; }

        /// <summary>
        /// If this server support subscription, the type that
        /// subscription operations will be rooted at.
        /// </summary>
        ObjectType SubscriptionType { get; }

        /// <summary>
        /// Gets all the schema types.
        /// </summary>
        IReadOnlyCollection<INamedType> Types { get; }

        /// <summary>
        /// Gets all the direcive types that are supported by this schema.
        /// </summary>
        IReadOnlyCollection<DirectiveType> DirectiveTypes { get; }

        /// <summary>
        /// Gets the session manager which can be used to create
        /// new query execution sessions.
        /// </summary>
        ISessionManager Sessions { get; }

        /// <summary>
        /// Gets a type by its name and kind.
        /// </summary>
        /// <typeparam name="T">The expected type kind.</typeparam>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>The type.</returns>
        /// <exception cref="ArgumentException">
        /// The specified type does not exist or is not of the specified type kind.
        /// </exception>
        T GetType<T>(NameString typeName)
            where T : INamedType;

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
        bool TryGetType<T>(NameString typeName, out T type)
            where T : INamedType;

        /// <summary>
        /// Tries to get the .net type representation of a schema.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="nativeType">The resolved .net type.</param>
        /// <returns>
        /// <c>true</c>, if a .net type was found that was bound
        /// the the specified schema type, <c>false</c> otherwise.
        /// </returns>
        bool TryGetNativeType(NameString typeName, out Type nativeType);

        /// <summary>
        /// Gets the possible object types to
        /// an abstract type (union type or interface type).
        /// </summary>
        /// <param name="abstractType">The abstract type.</param>
        /// <returns>
        /// Returns a collection with all possible object types
        /// for the given abstract type.
        /// </returns>
        IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType);

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
        DirectiveType GetDirectiveType(NameString directiveName);

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
        bool TryGetDirectiveType(
            NameString directiveName,
            out DirectiveType directiveType);
    }
}
