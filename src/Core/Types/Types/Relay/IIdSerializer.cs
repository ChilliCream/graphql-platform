#if !NETSTANDARD2_0
using System;
#endif

namespace HotChocolate.Types.Relay
{
    public interface IIdSerializer
    {
        /// <summary>
        /// Creates a schema unique identifier from an ID and type name.
        /// </summary>
        /// <typeparam name="T">The id type.</typeparam>
        /// <param name="typeName">The type name.</param>
        /// <param name="id">The id.</param>
        /// <returns>
        /// Returns an ID string containing the type name and the ID.
        /// </returns>
        /// <exception cref="IdSerializationException">
        /// Unable to create a schema unique ID string.
        /// </exception>
        string Serialize<T>(NameString typeName, T id);

        /// <summary>
        /// Creates a schema unique identifier from a source schema name,
        /// an ID and type name.
        /// </summary>
        /// <typeparam name="T">The id type.</typeparam>
        /// <param name="schemaName">The schema name.</param>
        /// <param name="typeName">The type name.</param>
        /// <param name="id">The id.</param>
        /// <returns>
        /// Returns an ID string containing the type name and the ID.
        /// </returns>
        /// <exception cref="IdSerializationException">
        /// Unable to create a schema unique ID string.
        /// </exception>
        string Serialize<T>(NameString schemaName, NameString typeName, T id);

        /// <summary>
        /// Deserializes a schema unique identifier to reveal the source
        /// schema, internal ID and type name of an object.
        /// </summary>
        /// <param name="serializedId">
        /// The schema unique ID string.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IdValue"/> containing the information
        /// encoded into the unique ID string.
        /// </returns>
        /// <exception cref="IdSerializationException">
        /// Unable to deconstruct the schema unique ID string.
        /// </exception>
        IdValue Deserialize(string serializedId);

#if !NETSTANDARD2_0
        /// <summary>
        /// Deserializes a schema unique identifier to reveal the source
        /// schema, internal ID and type name of an object.
        /// </summary>
        /// <param name="serializedId">
        /// The schema unique ID string.
        /// </param>
        /// <param name="resultType">
        /// An optional hint about the CLR type of the <see cref="IdValue.Value"/>.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IdValue"/> containing the information
        /// encoded into the unique ID string.
        /// </returns>
        /// <exception cref="IdSerializationException">
        /// Unable to deconstruct the schema unique ID string.
        /// </exception>
        IdValue Deserialize(string serializedId, Type resultType) =>
            // TODO: Remove default implementation at a major version bump
            Deserialize(serializedId);
#endif
    }
}
