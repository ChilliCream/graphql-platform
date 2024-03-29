#nullable enable

namespace HotChocolate.Types.Relay;

/// <summary>
/// The ID serializer is used to parse and format node ids.
/// </summary>
public interface IIdSerializer
{
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
    string? Serialize<T>(string? schemaName, string typeName, T id);

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
}
