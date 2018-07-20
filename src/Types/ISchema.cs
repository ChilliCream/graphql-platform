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
        /// Gets all the direcives that are supported by this schema.
        /// </summary>
        IReadOnlyCollection<Directive> Directives { get; }

        /// <summary>
        /// Gets the data loader descriptors.
        /// </summary>
        IReadOnlyCollection<DataLoaderDescriptor> DataLoaders { get; }

        /// <summary>
        /// Gets the state object descriptors.
        /// </summary>
        IReadOnlyCollection<StateObjectDescriptor> StateObjects { get; }

        /// <summary>
        /// Gets a type by its name and kind.
        /// </summary>
        /// <typeparam name="T">The expected type kind.</typeparam>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>The type.</returns>
        /// <exception cref="ArgumentException">
        /// The specified type does not exist or is not of the specified type kind.
        /// </exception>
        T GetType<T>(string typeName)
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
        bool TryGetType<T>(string typeName, out T type)
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
        bool TryGetNativeType(string typeName, out Type nativeType);

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
    }
}
