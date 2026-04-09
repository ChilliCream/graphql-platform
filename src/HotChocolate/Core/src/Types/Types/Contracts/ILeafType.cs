using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL leaf-type e.g., scalar or enum.
/// </summary>
public interface ILeafType : IInputTypeDefinition, IOutputTypeDefinition
{
    /// <summary>
    /// Determines if the given <paramref name="valueLiteral"/> is compatible with this type.
    /// </summary>
    /// <param name="valueLiteral">
    /// The GraphQL literal to validate.
    /// </param>
    /// <returns>
    /// <c>true</c> if the given <paramref name="valueLiteral"/> is compatible with this type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="valueLiteral"/> is <c>null</c>.
    /// </exception>
    bool IsValueCompatible(IValueNode valueLiteral);

    /// <summary>
    /// Determines if the given <paramref name="inputValue"/> is compatible with this type.
    /// </summary>
    /// <param name="inputValue">
    /// The deserialized JSON input value to validate.
    /// </param>
    /// <returns>
    /// <c>true</c> if the given <paramref name="inputValue"/> is compatible with this type.
    /// </returns>
    bool IsValueCompatible(JsonElement inputValue);

    /// <summary>
    /// Coerces a GraphQL literal (AST value node) into a runtime value.
    /// </summary>
    /// <param name="valueLiteral">
    /// The GraphQL literal to coerce.
    /// </param>
    /// <returns>
    /// Returns the runtime value representation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="valueLiteral"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="valueLiteral"/> into a runtime value.
    /// </exception>
    object CoerceInputLiteral(IValueNode valueLiteral);

    /// <summary>
    /// Coerces an external input value (deserialized JSON) into a runtime value.
    /// </summary>
    /// <param name="inputValue">
    /// The deserialized JSON input value to coerce.
    /// </param>
    /// <param name="context">
    /// Provides access to the coercion context.
    /// </param>
    /// <returns>
    /// Returns the runtime value representation.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="inputValue"/> into a runtime value.
    /// </exception>
    object CoerceInputValue(JsonElement inputValue, IFeatureProvider context);

    /// <summary>
    /// Coerces a runtime value into an external output representation
    /// and writes it to the result.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to coerce. Must not be <c>null</c>; null handling
    /// is the responsibility of the caller.
    /// </param>
    /// <param name="resultValue">
    /// The result element to write the output value to.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="runtimeValue"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="runtimeValue"/> into an output value.
    /// </exception>
    void CoerceOutputValue(object runtimeValue, ResultElement resultValue);

    /// <summary>
    /// Converts a runtime value into a GraphQL literal (AST value node).
    /// Used for default value representation in SDL and introspection.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to convert. Must not be <c>null</c>; null handling
    /// is the responsibility of the caller.
    /// </param>
    /// <returns>
    /// Returns a GraphQL literal representation of the runtime value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="runtimeValue"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="LeafCoercionException">
    /// Unable to convert the given <paramref name="runtimeValue"/> into a literal.
    /// </exception>
    IValueNode ValueToLiteral(object runtimeValue);
}
