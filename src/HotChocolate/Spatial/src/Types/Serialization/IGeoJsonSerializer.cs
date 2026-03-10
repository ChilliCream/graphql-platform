using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types.Spatial.Serialization;

/// <summary>
/// A serializer for GeoJSON types that handles coercion between GraphQL literals,
/// JSON elements, runtime values, and result values.
/// </summary>
internal interface IGeoJsonSerializer
{
    /// <summary>
    /// Determines if the given <paramref name="valueLiteral"/> is compatible with this type.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="valueLiteral">
    /// The GraphQL value literal which shall be validated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the given <paramref name="valueLiteral"/> is compatible with this type.
    /// </returns>
    bool IsValueCompatible(IType type, IValueNode valueLiteral);

    /// <summary>
    /// Determines if the given <paramref name="inputValue"/> is compatible with this type.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="inputValue">
    /// The JSON element which shall be validated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the given <paramref name="inputValue"/> is compatible with this type.
    /// </returns>
    bool IsValueCompatible(IType type, JsonElement inputValue);

    /// <summary>
    /// Coerces a GraphQL value literal into a runtime value.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="valueLiteral">
    /// A GraphQL value literal representation of this type.
    /// </param>
    /// <returns>
    /// Returns a runtime value representation of this type.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="valueLiteral"/>.
    /// </exception>
    object? CoerceInputLiteral(IType type, IValueNode valueLiteral);

    /// <summary>
    /// Coerces a JSON input value into a runtime value.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="inputValue">
    /// A JSON element representing the input value.
    /// </param>
    /// <param name="context">
    /// The feature provider context for accessing additional services.
    /// </param>
    /// <returns>
    /// Returns a runtime value representation of this type.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="inputValue"/>.
    /// </exception>
    object? CoerceInputValue(IType type, JsonElement inputValue, IFeatureProvider context);

    /// <summary>
    /// Coerces a runtime value into a result value and writes it to the result element.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// A runtime value representation of this type.
    /// </param>
    /// <param name="resultValue">
    /// The result element to write the output value to.
    /// </param>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="runtimeValue"/>.
    /// </exception>
    void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue);

    /// <summary>
    /// Converts a runtime value into a GraphQL value literal.
    /// </summary>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// A runtime value representation of this type.
    /// </param>
    /// <returns>
    /// Returns a GraphQL value literal representation of the <paramref name="runtimeValue"/>.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to convert the given <paramref name="runtimeValue"/>
    /// into a GraphQL value literal representation of this type.
    /// </exception>
    IValueNode ValueToLiteral(IType type, object? runtimeValue);

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
    /// Converts the coordinates of a geometry into a GraphQL value literal.
    /// </summary>
    /// <remarks>
    /// This is used for serializing complex geometries that consist of other geometries.
    /// </remarks>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// A runtime value representation of this type.
    /// </param>
    /// <returns>
    /// Returns a GraphQL value literal representing the coordinates.
    /// </returns>
    IValueNode CoordinateToLiteral(IType type, object? runtimeValue);

    /// <summary>
    /// Coerces the coordinates of a geometry and writes them to the result element.
    /// </summary>
    /// <remarks>
    /// This is used for serializing complex geometries that consist of other geometries.
    /// </remarks>
    /// <param name="type">
    /// The type for which we serialize.
    /// </param>
    /// <param name="runtimeValue">
    /// A runtime value representation of this type.
    /// </param>
    /// <param name="resultElement">
    /// The result element to write the coordinates to.
    /// </param>
    void CoerceOutputCoordinates(IType type, object runtimeValue, ResultElement resultElement);
}
