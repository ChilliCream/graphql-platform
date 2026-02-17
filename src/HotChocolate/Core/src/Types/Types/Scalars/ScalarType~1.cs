using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The .NET runtime type that this scalar represents.
/// </typeparam>
public abstract class ScalarType<TRuntimeType> : ScalarType where TRuntimeType : notnull
{
    /// <inheritdoc />
    protected ScalarType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    public sealed override Type RuntimeType => typeof(TRuntimeType);

    /// <inheritdoc />
    public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue is TRuntimeType t)
        {
            OnCoerceOutputValue(t, resultValue);
            return;
        }

        throw CreateCoerceOutputValueError(runtimeValue);
    }

    /// <summary>
    /// Coerces a runtime value into an external output representation
    /// and writes it to the result.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to coerce.
    /// </param>
    /// <param name="resultValue">
    /// The result element to write the output value to.
    /// </param>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="runtimeValue"/> into an output value.
    /// </exception>
    protected abstract void OnCoerceOutputValue(TRuntimeType runtimeValue, ResultElement resultValue);

    /// <inheritdoc />
    public override IValueNode ValueToLiteral(object runtimeValue)
    {
        if (runtimeValue is TRuntimeType runtimeType)
        {
            return OnValueToLiteral(runtimeType);
        }

        throw CreateValueToLiteralError(runtimeValue);
    }

    /// <summary>
    /// Converts a runtime value into a GraphQL literal (AST value node).
    /// Used for default value representation in SDL and introspection.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to convert.
    /// </param>
    /// <returns>
    /// Returns a GraphQL literal representation of the runtime value.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to convert the given <paramref name="runtimeValue"/> into a literal.
    /// </exception>
    protected abstract IValueNode OnValueToLiteral(TRuntimeType runtimeValue);

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceOutputValue(object, ResultElement)"/>
    /// encounters an incompatible runtime value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value that could not be coerced.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceOutputValueError(object runtimeValue)
        => Scalar_Cannot_CoerceOutputValue(this, runtimeValue);

    /// <summary>
    /// Creates the exception to throw when <see cref="ValueToLiteral(object)"/>
    /// encounters an incompatible runtime value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value that could not be converted to a literal.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateValueToLiteralError(object runtimeValue)
        => Scalar_Cannot_ConvertValueToLiteral(this, runtimeValue);
}
