using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// A parsable type is able to parse result and runtime values into GraphQL syntax and
    /// GraphQL syntax into runtime values.
    /// </summary>
    public interface IParsableType : IType
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
