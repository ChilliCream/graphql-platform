using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `LocalDate` scalar type represents a ISO date string, represented as UTF-8
/// character sequences YYYY-MM-DD. The scalar follows the specification defined in
/// <a href="https://tools.ietf.org/html/rfc3339">RFC3339</a>
/// </summary>
public class LocalDateType : ScalarType<DateOnly, StringValueNode>
{
    private const string _localFormat = "yyyy-MM-dd";

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    public LocalDateType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateType()
        : this(
            WellKnownScalarTypes.LocalDate,
            ScalarResources.LocalDateType_Description)
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,
            string s => new StringValueNode(s),
            DateOnly d => ParseValue(d),
            DateTimeOffset o => ParseValue(DateOnly.FromDateTime(o.DateTime)),
            DateTime dt => ParseValue(DateOnly.FromDateTime(dt)),
            _ => throw ThrowHelper.LocalDateType_ParseValue_IsInvalid(this),
        };
    }

    protected override DateOnly ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, out var value))
        {
            return value.Value;
        }

        throw ThrowHelper.LocalDateType_ParseLiteral_IsInvalid(this);
    }

    protected override StringValueNode ParseValue(DateOnly runtimeValue)
    {
        return new(Serialize(runtimeValue));
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        switch (runtimeValue)
        {
            case null:
                resultValue = null;
                return true;
            case DateOnly d:
                resultValue = Serialize(d);
                return true;
            case DateTimeOffset o:
                resultValue = Serialize(o);
                return true;
            case DateTime dt:
                resultValue = Serialize(dt);
                return true;
            default:
                resultValue = null;
                return false;
        }
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        switch (resultValue)
        {
            case null:
                runtimeValue = null;
                return true;
            case string s when TryDeserializeFromString(s, out var d):
                runtimeValue = d;
                return true;
            case DateOnly d:
                runtimeValue = d;
                return true;
            case DateTimeOffset o:
                runtimeValue = DateOnly.FromDateTime(o.DateTime);
                return true;
            case DateTime dt:
                runtimeValue = DateOnly.FromDateTime(dt);
                return true;
            default:
                runtimeValue = null;
                return false;
        }
    }

    private static string Serialize(IFormattable value)
    {
        return value.ToString(_localFormat, CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateOnly? value)
    {
        if (serialized is not null
            && DateOnly.TryParseExact(
                serialized,
                _localFormat,
                out var date))
        {
            value = date;
            return true;
        }

        value = null;
        return false;
    }
}
