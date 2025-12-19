using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The .NET runtime type that this scalar represents.
/// </typeparam>
public abstract class ScalarType<TRuntimeType> : ScalarType
{
    /// <inheritdoc />
    protected ScalarType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    public sealed override Type RuntimeType => typeof(TRuntimeType);

    /// <inheritdoc />
    public override bool IsInstanceOfType(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return true;
        }

        return RuntimeType.IsInstanceOfType(runtimeValue);
    }

    /// <inheritdoc />
    public override void CoerceOutputValue(object? runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue.SetNullValue();
            return;
        }

        if (runtimeValue is TRuntimeType t)
        {
            CoerceOutputValue(t, resultValue);
            return;
        }

        throw new LeafCoercionException(
            TypeResourceHelper.Scalar_Cannot_CoerceOutputValue(
                runtimeValue.GetType(),
                runtimeValue.ToString() ?? "null"),
            this);
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
    public abstract void CoerceOutputValue(TRuntimeType? runtimeValue, ResultElement resultValue);

    /// <inheritdoc />
    public override IValueNode ValueToLiteral(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is TRuntimeType runtimeType)
        {
            return ValueToLiteral(runtimeType);
        }

        throw new LeafCoercionException(
            TypeResourceHelper.Scalar_Cannot_ConvertValueToLiteral(
                runtimeValue.GetType(),
                runtimeValue.ToString() ?? "null"),
            this);
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
    public abstract IValueNode ValueToLiteral(TRuntimeType? runtimeValue);
}
