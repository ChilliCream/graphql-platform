using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL leaf-type e.g. scalar or enum.
/// </summary>
public interface ILeafType
    : INamedOutputType
    , INamedInputType
{
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
    /// <returns>
    /// Returns a runtime value representation of this type.
    /// </returns>
    object? ParseLiteral(IValueNode valueSyntax);

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

    bool TryDeserialize(object? resultValue, out object? runtimeValue);
}
