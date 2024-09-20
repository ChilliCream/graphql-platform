using HotChocolate.Language;

namespace HotChocolate.Types.Spatial.Serialization;

/// <summary>
/// A serializable type can serialize its runtime value to the result value
/// format and deserialize the result value format back to its runtime value.
/// </summary>
internal interface IGeoJsonSerializer
{
    /// <summary>
    /// Defines if the given <paramref name="valueSyntax"/> is possibly of this type.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="valueSyntax">
    /// The GraphQL value syntax which shall be validated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the given <paramref name="valueSyntax"/> is possibly of this type.
    /// </returns>
    bool IsInstanceOfType(IType type, IValueNode valueSyntax);

    /// <summary>
    /// Defines if the given <paramref name="runtimeValue"/> is possibly of this type.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// The runtime value which shall be validated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the given <paramref name="runtimeValue"/> is possibly of this type.
    /// </returns>
    bool IsInstanceOfType(IType type, object? runtimeValue);

    /// <summary>
    /// Serializes a runtime value of this type to the result value format.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// A runtime value representation of this type.
    /// </param>
    /// <returns>
    /// Returns a result value representation of this type.
    /// </returns>
    /// <exception cref="SerializationException">
    /// Unable to serialize the given <paramref name="runtimeValue"/>.
    /// </exception>
    object? Serialize(IType type, object? runtimeValue);

    /// <summary>
    /// Deserializes a result value of this type to the runtime value format.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="resultValue">
    /// A result value representation of this type.
    /// </param>
    /// <returns>
    /// Returns a runtime value representation of this type.
    /// </returns>
    object? Deserialize(IType type, object? resultValue);

    /// <summary>
    /// Try to deserialize a result value of this type to the runtime value format.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="resultValue">
    /// A result value representation of this type.
    /// </param>
    /// <param name="runtimeValue">
    /// Returns a runtime value representation of this type.
    /// </param>
    /// <returns>True if deserializing was successful</returns>
    bool TryDeserialize(IType type, object? resultValue, out object? runtimeValue);

    /// <summary>
    /// Serializes a runtime value of this type to the result value format.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// Returns a result value representation of this type.
    /// </param>
    /// <param name="resultValue">
    /// A runtime value representation of this type.
    /// </param>
    /// <returns>True if serializing was successful</returns>
    bool TrySerialize(
        IType type,
        object? runtimeValue,
        out object? resultValue);

    /// <summary>
    /// Parses the GraphQL value syntax of this type into a runtime value representation.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="valueSyntax">
    /// A GraphQL value syntax representation of this type.
    /// </param>
    /// <returns>
    /// Returns a runtime value representation of this type.
    /// </returns>
    object? ParseLiteral(IType type, IValueNode valueSyntax);

    /// <summary>
    /// Parses a runtime value of this type into a GraphQL value syntax representation.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// A result value representation of this type.
    /// </param>
    /// <returns>
    /// Returns a GraphQL value syntax representation of the <paramref name="runtimeValue"/>.
    /// </returns>
    /// <exception cref="SerializationException">
    /// Unable to parse the given <paramref name="runtimeValue"/>
    /// into a GraphQL value syntax representation of this type.
    /// </exception>
    IValueNode ParseValue(IType type, object? runtimeValue);

    /// <summary>
    /// Parses a result value of this into a GraphQL value syntax representation.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="resultValue">
    /// A result value representation of this type.
    /// </param>
    /// <returns>
    /// Returns a GraphQL value syntax representation of the <paramref name="resultValue"/>.
    /// </returns>
    /// <exception cref="SerializationException">
    /// Unable to parse the given <paramref name="resultValue"/>
    /// into a GraphQL value syntax representation of this type.
    /// </exception>
    IValueNode ParseResult(IType type, object? resultValue);

    /// <summary>
    /// Creates a new runtime value from already parsed field values.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="fieldValues">The parsed field values.</param>
    /// <returns>
    /// Returns the runtime value.
    /// </returns>
    object CreateInstance(IType type, object?[] fieldValues);

    /// <summary>
    /// Copies the field values from the provided runtime value.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">The runtime value.</param>
    /// <param name="fieldValues">The field values array.</param>
    void GetFieldData(IType type, object runtimeValue, object?[] fieldValues);

    /// <summary>
    /// Tries to serialize the `coordinates` field of the geometry
    /// </summary>
    /// <remarks>
    /// This is used for serializing complex geometries that consist of other geometries
    /// </remarks>
    /// <param name="type"></param>
    /// <param name="runtimeValue"></param>
    /// <param name="serialized"></param>
    /// <returns></returns>
    bool TrySerializeCoordinates(
        IType type,
        object runtimeValue,
        out object? serialized);

    /// <summary>
    /// Tries to parse the `coordinates` field of the geometry
    /// </summary>
    /// <remarks>
    /// This is used for serializing complex geometries that consist of other geometries
    /// </remarks>
    /// <param name="type"></param>
    /// <param name="runtimeValue"></param>
    /// <returns></returns>
    IValueNode ParseCoordinateValue(IType type, object? runtimeValue);

    /// <summary>
    /// Tries to parse the `coordinates` field of the geometry
    /// </summary>
    /// <remarks>
    /// This is used for serializing complex geometries that consist of other geometries
    /// </remarks>
    /// <param name="type"></param>
    /// <param name="runtimeValue"></param>
    /// <returns></returns>
    IValueNode ParseCoordinateResult(IType type, object? runtimeValue);
}
