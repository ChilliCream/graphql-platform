using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for byte arrays that are serialized as Base64-encoded strings in GraphQL.
/// This type handles the conversion between byte arrays in .NET and string representations in GraphQL schemas.
/// </summary>
public class ByteArrayType : ScalarType<byte[], StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
    /// </summary>
    public ByteArrayType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        SerializationType = ScalarSerializationType.String;
        Pattern = @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ByteArrayType()
        : this(ScalarNames.ByteArray, bind: BindingBehavior.Implicit)
    {
    }

    protected override byte[] ParseLiteral(StringValueNode valueSyntax)
    {
        return Convert.FromBase64String(valueSyntax.Value);
    }

    protected override StringValueNode ParseValue(byte[] runtimeValue)
    {
        return new(Convert.ToBase64String(runtimeValue));
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

        if (resultValue is byte[] b)
        {
            return ParseValue(b);
        }

        throw new LeafCoercionException(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
            this);
    }

    public override bool TryCoerceOutputValue(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is byte[] b)
        {
            resultValue = Convert.ToBase64String(b);
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

        if (resultValue is string s)
        {
            runtimeValue = Convert.FromBase64String(s);
            return true;
        }

        if (resultValue is byte[] b)
        {
            runtimeValue = b;
            return true;
        }

        runtimeValue = null;
        return false;
    }
}
