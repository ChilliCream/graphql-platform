using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// The `LocalTime` scalar type is a local time string (i.e., with no associated timezone)
/// in 24-hr HH:mm:ss.
/// </summary>
public class LocalTimeType : ScalarType<TimeOnly, StringValueNode>
{
    private const string LocalFormat = "HH:mm:ss";

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    public LocalTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalTimeType()
        : this(
            ScalarNames.LocalTime,
            TypeResources.LocalTimeType_Description)
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,
            string s => new StringValueNode(s),
            TimeOnly t => ParseValue(t),
            DateTimeOffset d => ParseValue(TimeOnly.FromDateTime(d.DateTime)),
            DateTime dt => ParseValue(TimeOnly.FromDateTime(dt)),
            _ => throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()), this)
        };
    }

    protected override TimeOnly ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, out var value))
        {
            return value.Value;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(TimeOnly runtimeValue)
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
            case TimeOnly t:
                resultValue = Serialize(t);
                return true;
            case DateTimeOffset dt:
                resultValue = Serialize(dt);
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
            case string s when TryDeserializeFromString(s, out var t):
                runtimeValue = t;
                return true;
            case TimeOnly t:
                runtimeValue = t;
                return true;
            case DateTimeOffset d:
                runtimeValue = TimeOnly.FromDateTime(d.DateTime);
                return true;
            case DateTime d:
                runtimeValue = TimeOnly.FromDateTime(d);
                return true;
            default:
                runtimeValue = null;
                return false;
        }
    }

    private static string Serialize(IFormattable value)
    {
        return value.ToString(LocalFormat, CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out TimeOnly? value)
    {
        if (serialized is not null
            && TimeOnly.TryParseExact(
                serialized,
                LocalFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
        {
            value = time;
            return true;
        }

        value = null;
        return false;
    }
}
