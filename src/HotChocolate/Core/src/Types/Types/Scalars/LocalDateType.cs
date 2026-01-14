using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// This scalar represents a date without a time-zone in the
/// <see href="https://en.wikipedia.org/wiki/ISO_8601">ISO-8601</see> calendar system.
/// </para>
/// <para>
/// The pattern is "YYYY-MM-DD" with "YYYY" representing the year, "MM" the month, and "DD" the day.
/// </para>
/// </summary>
/// <seealso href="https://scalars.graphql.org/andimarek/local-date.html">Specification</seealso>
public class LocalDateType : ScalarType<DateOnly, StringValueNode>
{
    private const string LocalFormat = "yyyy-MM-dd";
    private const string SpecifiedByUri = "https://scalars.graphql.org/andimarek/local-date.html";
    private readonly bool _enforceSpecFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    public LocalDateType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false)
        : base(name, bind)
    {
        Description = description;
        SerializationType = ScalarSerializationType.String;
        Pattern = @"^\d{4}-\d{2}-\d{2}$";
        SpecifiedBy = new Uri(SpecifiedByUri);
        _enforceSpecFormat = !disableFormatCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    public LocalDateType(bool disableFormatCheck)
        : this(
            ScalarNames.LocalDate,
            TypeResources.LocalDateType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateType()
        : this(
            ScalarNames.LocalDate,
            TypeResources.LocalDateType_Description)
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
            _ => throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()), this)
        };
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
        return value.ToString(LocalFormat, CultureInfo.InvariantCulture);
    }

    private bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateOnly? value)
    {
        if (_enforceSpecFormat)
        {
            if (DateOnly.TryParseExact(
                serialized,
                LocalFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                value = date;
                return true;
            }
        }
        else if (DateOnly.TryParse(
            serialized,
            CultureInfo.InvariantCulture,
            out var date))
        {
            value = date;
            return true;
        }

        value = null;
        return false;
    }
}
