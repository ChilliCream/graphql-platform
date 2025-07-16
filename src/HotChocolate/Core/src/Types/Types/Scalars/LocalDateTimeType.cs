using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The `LocalDateTime` scalar type is a local date/time string (i.e., with no associated timezone)
/// with the format `YYYY-MM-DDThh:mm:ss`.
/// </summary>
public class LocalDateTimeType : ScalarType<DateTime, StringValueNode>
{
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss";

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    public LocalDateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateTimeType()
        : this(
            ScalarNames.LocalDateTime,
            TypeResources.LocalDateTimeType_Description)
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,
            string s => new StringValueNode(s),
            DateTimeOffset o => ParseValue(o.DateTime),
            DateTime dt => ParseValue(dt),
            _ => throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()), this)
        };
    }

    protected override DateTime ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, out var value))
        {
            return value.Value;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(DateTime runtimeValue)
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
            case DateTimeOffset o:
                runtimeValue = o.DateTime;
                return true;
            case DateTime dt:
                runtimeValue = dt;
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
        [NotNullWhen(true)] out DateTime? value)
    {
        if (serialized is not null
            && DateTime.TryParseExact(
                serialized,
                LocalFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateTime))
        {
            value = dateTime;
            return true;
        }

        value = null;
        return false;
    }
}
