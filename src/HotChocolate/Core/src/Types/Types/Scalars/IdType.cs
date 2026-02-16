using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// The ID scalar type represents a unique identifier, often used to refetch
/// an object or as the key for a cache. The ID type is serialized in the
/// same way as a String; however, it is not intended to be human‐readable.
/// </para>
/// <para>While it is often numeric, it should always serialize as a String.</para>
/// <para>http://facebook.github.io/graphql/June2018/#sec-ID</para>
/// </summary>
public class IdType : ScalarType<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdType"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar type.
    /// </param>
    /// <param name="description">
    /// The description of the scalar type.
    /// </param>
    /// <param name="bind">
    /// The binding behavior of this scalar.
    /// </param>
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

    /// <inheritdoc />
    public override ScalarSerializationType SerializationType
        => ScalarSerializationType.String | ScalarSerializationType.Int;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is StringValueNode or IntValueNode;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.String or JsonValueKind.Number;

    /// <inheritdoc />
    public override object CoerceInputLiteral(IValueNode literal)
    {
        if (literal is StringValueNode stringLiteral)
        {
            return stringLiteral.Value;
        }

        if (literal is IntValueNode intLiteral)
        {
            return Encoding.UTF8.GetString(intLiteral.AsSpan());
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, literal);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Accepts JSON strings and integer numbers. Floating-point numbers
    /// (containing '.', 'e', or 'E') are rejected.
    /// </remarks>
    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.String)
        {
            return inputValue.GetString()!;
        }

        if (inputValue.ValueKind is JsonValueKind.Number)
        {
            var rawValue = JsonMarshal.GetRawUtf8Value(inputValue);

            // Only accept integers; reject floating-point numbers
            if (rawValue.IndexOfAny((byte)'.', (byte)'e', (byte)'E') == -1)
            {
                return Encoding.UTF8.GetString(rawValue);
            }
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    public override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue);

    /// <inheritdoc />
    public override IValueNode OnValueToLiteral(string runtimeValue)
        => new StringValueNode(runtimeValue);
}
