using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
public abstract class ScalarType<TRuntimeType, TLiteral>
    : ScalarType<TRuntimeType>
    where TLiteral : IValueNode
{
    /// <inheritdoc />
    protected ScalarType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    public sealed override bool IsInstanceOfType(IValueNode valueSyntax)
    {
        if (valueSyntax is null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        return valueSyntax is TLiteral casted && IsInstanceOfType(casted)
            || valueSyntax is NullValueNode;
    }

    /// <summary>
    /// Defines if the specified <paramref name="valueSyntax" />
    /// can be parsed by this scalar.
    /// </summary>
    /// <param name="valueSyntax">
    /// The literal that shall be checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the literal can be parsed by this scalar;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="valueSyntax" /> is <c>null</c>.
    /// </exception>
    protected virtual bool IsInstanceOfType(TLiteral valueSyntax)
    {
        return true;
    }

    /// <inheritdoc />
    public sealed override bool IsInstanceOfType(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return true;
        }

        if (runtimeValue is TRuntimeType t)
        {
            return IsInstanceOfType(t);
        }

        return false;
    }

    /// <summary>
    /// Defines if the specified <paramref name="runtimeValue" />
    /// is a instance of this type.
    /// </summary>
    /// <param name="runtimeValue">
    /// A value representation of this type.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value is a value of this type;
    /// otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool IsInstanceOfType(TRuntimeType runtimeValue)
    {
        return true;
    }

    /// <inheritdoc />
    public sealed override object? ParseLiteral(IValueNode valueSyntax)
    {
        if (valueSyntax is null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        if (valueSyntax is TLiteral casted && IsInstanceOfType(casted))
        {
            return ParseLiteral(casted);
        }

        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        throw CreateParseLiteralError(valueSyntax);
    }

    /// <summary>
    /// Parses the specified <paramref name="valueSyntax" />
    /// to the .net representation of this type.
    /// </summary>
    /// <param name="valueSyntax">
    /// The literal that shall be parsed.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="valueSyntax" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="SerializationException">
    /// The specified <paramref name="valueSyntax" /> cannot be parsed
    /// by this scalar.
    /// </exception>
    protected abstract TRuntimeType ParseLiteral(TLiteral valueSyntax);

    /// <inheritdoc />
    public sealed override IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is TRuntimeType t && IsInstanceOfType(t))
        {
            return ParseValue(t);
        }

        throw CreateParseValueError(runtimeValue);
    }

    /// <summary>
    /// Parses a runtime value into a valueSyntax.
    /// </summary>
    /// <param name="runtimeValue">The value to parse</param>
    /// <returns>The parsed value syntax</returns>
    protected abstract TLiteral ParseValue(TRuntimeType runtimeValue);

    /// <summary>
    /// Creates the exception that will be thrown when <see cref="ParseLiteral(IValueNode)"/> encountered an
    /// invalid <see cref="IValueNode "/>
    /// </summary>
    /// <param name="valueSyntax">
    /// The value syntax that should be parsed
    /// </param>
    /// <returns>
    /// The created exception that should be thrown
    /// </returns>
    protected virtual SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return new(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    /// <summary>
    /// Creates the exception that will be thrown when <see cref="ParseValue(object?)"/> encountered an
    /// invalid value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtimeValue that should be parsed
    /// </param>
    /// <returns>
    /// The created exception that should be thrown
    /// </returns>
    protected virtual SerializationException CreateParseValueError(object runtimeValue)
    {
        return new(
            TypeResourceHelper.Scalar_Cannot_ParseValue(Name, runtimeValue.GetType()),
            this);
    }
}
