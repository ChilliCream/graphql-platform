using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The <c>UUID</c> scalar type represents a Universally Unique Identifier (UUID) as defined by RFC
/// 9562. It is intended for scenarios where globally unique identifiers are required, such as
/// database primary keys, distributed system identifiers, or any situation requiring
/// collision-resistant unique identifiers.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/uuid.html">Specification</seealso>
public class UuidType : ScalarType<Guid, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/uuid.html";
    private readonly string _format;
    private readonly bool _enforceFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidType"/> class.
    /// </summary>
    /// <param name="defaultFormat">
    /// The expected format of GUID strings by this scalar.
    /// <c>'N'</c>: nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
    /// <c>'D'</c> (default): nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
    /// <c>'B'</c>: {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
    /// <c>'P'</c>: (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
    /// </param>
    /// <param name="enforceFormat">
    /// Specifies if the <paramref name="defaultFormat"/> is enforced and violations will cause
    /// a <see cref="LeafCoercionException"/>. If set to <c>false</c> and the string
    /// does not match the <paramref name="defaultFormat"/> the scalar will try to deserialize
    /// the string using the other formats.
    /// </param>
    public UuidType(char defaultFormat = '\0', bool enforceFormat = false)
        : this(
            ScalarNames.UUID,
            TypeResources.UuidType_Description,
            defaultFormat,
            enforceFormat,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidType"/> class.
    /// </summary>
    /// <param name="name">
    /// The name that this scalar shall have.
    /// </param>
    /// <param name="description">
    /// The description of this scalar.
    /// </param>
    /// <param name="defaultFormat">
    /// The expected format of GUID strings by this scalar.
    /// <c>'N'</c>: nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
    /// <c>'D'</c> (default): nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
    /// <c>'B'</c>: {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
    /// <c>'P'</c>: (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
    /// </param>
    /// <param name="enforceFormat">
    /// Specifies if the <paramref name="defaultFormat"/> is enforced and violations will cause
    /// a <see cref="LeafCoercionException"/>. If set to <c>false</c> and the string
    /// does not match the <paramref name="defaultFormat"/> the scalar will try to deserialize
    /// the string using the other formats.
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar binds implicitly to <see cref="Guid"/>,
    /// or must be explicitly bound.
    /// </param>
    public UuidType(
        string name,
        string? description = null,
        char defaultFormat = '\0',
        bool enforceFormat = false,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
        _format = CreateFormatString(defaultFormat);
        _enforceFormat = enforceFormat;

        Pattern = _format switch
        {
            "B" => @"^\{[\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}\}$",
            "D" => @"^[\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}$",
            "N" => @"^[\da-fA-F]{32}$",
            "P" => @"^\([\da-fA-F]{8}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{4}-[\da-fA-F]{12}\)$",
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UuidType() : this('\0')
    {
    }

    /// <inheritdoc />
    protected override Guid OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseGuid(valueLiteral.Value, valueLiteral.AsSpan(), out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override Guid OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        var stringValue = inputValue.GetString()!;
        var bytes = Encoding.UTF8.GetBytes(stringValue);

        if (TryParseGuid(stringValue, bytes, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(Guid runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(_format));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(Guid runtimeValue)
        => new StringValueNode(runtimeValue.ToString(_format));

    private bool TryParseGuid(string stringValue, ReadOnlySpan<byte> bytes, out Guid value)
    {
        if (_enforceFormat)
        {
            if (Utf8Parser.TryParse(bytes, out Guid guid, out var consumed, _format[0])
                && consumed == bytes.Length)
            {
                value = guid;
                return true;
            }
        }
        else if (Guid.TryParse(stringValue, out var guid))
        {
            value = guid;
            return true;
        }

        value = default;
        return false;
    }

    private static string CreateFormatString(char format)
    {
        if (format != '\0'
            && format != 'N'
            && format != 'D'
            && format != 'B'
            && format != 'P')
        {
            throw new ArgumentException(TypeResources.UuidType_FormatUnknown, nameof(format));
        }

        return format == '\0' ? "D" : format.ToString();
    }
}
