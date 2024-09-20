using System.Buffers.Text;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public class UuidType : ScalarType<Guid, StringValueNode>
{
    private const string _specifiedBy = "https://tools.ietf.org/html/rfc4122";
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
    /// a <see cref="SerializationException"/>. If set to <c>false</c> and the string
    /// does not match the <paramref name="defaultFormat"/> the scalar will try to deserialize
    /// the string using the other formats.
    /// </param>
    public UuidType(char defaultFormat = '\0', bool enforceFormat = false)
        : this(
            ScalarNames.UUID,
            defaultFormat: defaultFormat,
            enforceFormat: enforceFormat,
            bind: BindingBehavior.Implicit)
    {
        SpecifiedBy = new Uri(_specifiedBy);
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
    /// a <see cref="SerializationException"/>. If set to <c>false</c> and the string
    /// does not match the <paramref name="defaultFormat"/> the scalar will try to deserialize
    /// the string using the other formats.
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar binds implicitly to <see cref="System.Guid"/>,
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
        _format = CreateFormatString(defaultFormat);
        _enforceFormat = enforceFormat;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UuidType() : this('\0')
    {
    }

    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
    {
        if (_enforceFormat)
        {
            var value = valueSyntax.AsSpan();

            if (Utf8Parser.TryParse(value, out Guid _, out var consumed, _format[0]) &&
                consumed == value.Length)
            {
                return true;
            }
        }
        else if (Guid.TryParse(valueSyntax.Value, out _))
        {
            return true;
        }

        return false;
    }

    protected override Guid ParseLiteral(StringValueNode valueSyntax)
    {
        if (_enforceFormat)
        {
            var value = valueSyntax.AsSpan();

            if (Utf8Parser.TryParse(value, out Guid g, out var consumed, _format[0]) &&
                consumed == value.Length)
            {
                return g;
            }
        }
        else if (Guid.TryParse(valueSyntax.Value, out var g))
        {
            return g;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(Guid runtimeValue)
    {
        return new(runtimeValue.ToString(_format));
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

        if (resultValue is Guid g)
        {
            return ParseValue(g);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, resultValue.GetType()),
            this);
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is Guid guid)
        {
            resultValue = guid.ToString(_format);
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
            var bytes = Encoding.UTF8.GetBytes(s);

            if (_enforceFormat &&
                Utf8Parser.TryParse(bytes, out Guid guid, out var consumed, _format[0]) &&
                consumed == bytes.Length)
            {
                runtimeValue = guid;
                return true;
            }

            if (!_enforceFormat && Guid.TryParse(s, out guid))
            {
                runtimeValue = guid;
                return true;
            }
        }

        if (resultValue is Guid)
        {
            runtimeValue = resultValue;
            return true;
        }

        runtimeValue = null;
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
