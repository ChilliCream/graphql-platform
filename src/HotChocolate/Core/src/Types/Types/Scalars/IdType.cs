using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The ID scalar type represents a unique identifier, often used to refetch
/// an object or as the key for a cache. The ID type is serialized in the
/// same way as a String; however, it is not intended to be human‐readable.
///
/// While it is often numeric, it should always serialize as a String.
///
/// http://facebook.github.io/graphql/June2018/#sec-ID
/// </summary>
[SpecScalar]
public class IdType : ScalarType<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdType"/> class.
    /// </summary>
    public IdType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public IdType() : this(ScalarNames.ID, TypeResources.IdType_Description)
    {
    }

    public override bool IsInstanceOfType(IValueNode literal)
    {
        if (literal is null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        return literal is StringValueNode
            || literal is IntValueNode
            || literal is NullValueNode;
    }

    public override object? ParseLiteral(IValueNode literal)
    {
        if (literal is null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        if (literal is StringValueNode stringLiteral)
        {
            return stringLiteral.Value;
        }

        if (literal is IntValueNode intLiteral)
        {
            return intLiteral.Value;
        }

        if (literal is NullValueNode)
        {
            return null;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, literal.GetType()),
            this);
    }

    public override IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is string s)
        {
            return new StringValueNode(s);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseValue(Name, runtimeValue.GetType()),
            this);
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s)
        {
            return new StringValueNode(s);
        }

        if (resultValue is int i)
        {
            return new IntValueNode(i);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
            this);
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is string)
        {
            resultValue = runtimeValue;
            return true;
        }

        resultValue = null;
        return false;
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is string)
        {
            runtimeValue = resultValue;
            return true;
        }

        if (TryConvertSerialized(resultValue, ValueKind.Integer, out string c))
        {
            runtimeValue = c;
            return true;
        }

        runtimeValue = null;
        return false;
    }
}
