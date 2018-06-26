using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchema
    {
        IReadOnlySchemaOptions Options { get; }

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


        Field GetField(INamedType namedType, string name);

        bool TryGetField(INamedType namedType, string name, out Field field);


        IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType);


    }
}