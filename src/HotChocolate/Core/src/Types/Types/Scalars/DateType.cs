using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public class DateType : ScalarType<DateOnly, StringValueNode>
{
    private const string _dateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Initializes a new instance of the <see cref="DateType"/> class.
    /// </summary>
    public DateType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DateType() : this(ScalarNames.Date, TypeResources.DateType_Description)
    {
    }

    protected override DateOnly ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, out var value))
        {
            return value.Value;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(DateOnly runtimeValue) =>
        new(Serialize(runtimeValue));

    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,
            string s => new StringValueNode(s),
            DateOnly d => ParseValue(d),
            DateTimeOffset o => ParseValue(DateOnly.FromDateTime(o.UtcDateTime)),
            DateTime dt => ParseValue(DateOnly.FromDateTime(dt.ToUniversalTime())),
            _ => throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()), this)
        };
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
                resultValue = Serialize(o.UtcDateTime);
                return true;
            case DateTime dt:
                resultValue = Serialize(dt.ToUniversalTime());
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
                runtimeValue = DateOnly.FromDateTime(o.UtcDateTime);
                return true;
            case DateTime dt:
                runtimeValue = DateOnly.FromDateTime(dt.ToUniversalTime());
                return true;
            default:
                runtimeValue = null;
                return false;
        }
    }

    private static string Serialize(IFormattable value) =>
        value.ToString(_dateFormat, CultureInfo.InvariantCulture);

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateOnly? value)
    {
        if (DateOnly.TryParseExact(
           serialized,
           _dateFormat,
           out var date))
        {
            value = date;
            return true;
        }

        value = null;
        return false;
    }
}
