using HotChocolate.Language;

namespace HotChocolate.Types.Spatial.Serialization
{
    /// <summary>
    /// A serializable type can serialize its runtime value to the result value
    /// format and deserialize the result value format back to its runtime value.
    /// </summary>
    internal interface IGeoJsonSerializer
    {
        /// <summary>
        /// Serializes a runtime value of this type to the result value format.
        /// </summary>
        /// <param name="runtimeValue">
        /// A runtime value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a result value representation of this type.
        /// </returns>
        /// <exception cref="SerializationException">
        /// Unable to serialize the given <paramref name="runtimeValue"/>.
        /// </exception>
        object? Serialize(object? runtimeValue);

        /// <summary>
        /// Deserializes a result value of this type to the runtime value format.
        /// </summary>
        /// <param name="resultValue">
        /// A result value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a runtime value representation of this type.
        /// </returns>
        object? Deserialize(object? resultValue);

        /// <summary>
        /// Try to deserialize a result value of this type to the runtime value format.
        /// </summary>
        /// <param name="resultValue">
        /// A result value representation of this type.
        /// </param>
        /// <param name="runtimeValue">
        /// Returns a runtime value representation of this type.
        /// </param>
        /// <returns>True if deserializing was successful</returns>
        bool TryDeserialize(object? resultValue, out object? runtimeValue);

        /// <summary>
        /// Serializes a runtime value of this type to the result value format.
        /// </summary>
        /// <param name="resultValue">
        /// A runtime value representation of this type.
        /// </param>
        /// <param name="runtimeValue">
        /// Returns a result value representation of this type.
        /// </param>
        /// <returns>True if serializing was successful</returns>
        bool TrySerialize(object? runtimeValue, out object? resultValue);

        /// <summary>
        /// Defines if the given <paramref name="valueSyntax"/> is possibly of this type.
        /// </summary>
        /// <param name="valueSyntax">
        /// The GraphQL value syntax which shall be validated.
        /// </param>
        /// <returns>
        /// <c>true</c> if the given <paramref name="valueSyntax"/> is possibly of this type.
        /// </returns>
        bool IsInstanceOfType(IValueNode valueSyntax);

        /// <summary>
        /// Defines if the given <paramref name="runtimeValue"/> is possibly of this type.
        /// </summary>
        /// <param name="runtimeValue">
        /// The runtime value which shall be validated.
        /// </param>
        /// <returns>
        /// <c>true</c> if the given <paramref name="runtimeValue"/> is possibly of this type.
        /// </returns>
        bool IsInstanceOfType(object? runtimeValue);

        /// <summary>
        /// Parses the GraphQL value syntax of this type into a runtime value representation.
        /// </summary>
        /// <param name="valueSyntax">
        /// A GraphQL value syntax representation of this type.
        /// </param>
        /// <param name="withDefaults">
        /// Specifies if default values shall be used if a field value us not provided.
        /// </param>
        /// <returns>
        /// Returns a runtime value representation of this type.
        /// </returns>
        object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true);

        /// <summary>
        /// Parses a runtime value of this type into a GraphQL value syntax representation.
        /// </summary>
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
        IValueNode ParseValue(object? runtimeValue);

        /// <summary>
        /// Parses a result value of this into a GraphQL value syntax representation.
        /// </summary>
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
        IValueNode ParseResult(object? resultValue);
    }
}
