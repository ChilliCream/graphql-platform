using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// This GraphQL Scalar represents an exact point in time.
/// This point in time is specified by having an offset to UTC and does not use time zone.
///
/// https://www.graphql-scalars.com/date-time/
/// </summary>
public class DateTimeType : ScalarType<DateTimeOffset, StringValueNode>
{
    private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
    private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";
    private const string _specifiedBy = "https://www.graphql-scalars.com/date-time";

    private static readonly Regex DateTimeScalarRegex = new(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|[+-][0-9]{2}:[0-9]{2})$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    public DateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(_specifiedBy);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DateTimeType()
        : this(
            ScalarNames.DateTime,
            TypeResources.DateTimeType_Description,
            BindingBehavior.Implicit)
    {
    }

    protected override DateTimeOffset ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, out var value))
        {
            return value.Value;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(DateTimeOffset runtimeValue)
    {
        return new(Serialize(runtimeValue));
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

        if (resultValue is DateTimeOffset d)
        {
            return ParseValue(d);
        }

        if (resultValue is DateTime dt)
        {
            return ParseValue(new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero));
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

        if (runtimeValue is DateTimeOffset dt)
        {
            resultValue = Serialize(dt);
            return true;
        }

        if (runtimeValue is DateTime d)
        {
            resultValue = Serialize(new DateTimeOffset(d.ToUniversalTime(), TimeSpan.Zero));
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

        if (resultValue is string s && TryDeserializeFromString(s, out var d))
        {
            runtimeValue = d;
            return true;
        }

        if (resultValue is DateTimeOffset)
        {
            runtimeValue = resultValue;
            return true;
        }

        if (resultValue is DateTime dt)
        {
            runtimeValue = new DateTimeOffset(
                dt.ToUniversalTime(),
                TimeSpan.Zero);
            return true;
        }

        runtimeValue = null;
        return false;
    }

    private static string Serialize(DateTimeOffset value)
    {
        if (value.Offset == TimeSpan.Zero)
        {
            return value.ToString(
                _utcFormat,
                CultureInfo.InvariantCulture);
        }

        return value.ToString(
            _localFormat,
            CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateTimeOffset? value)
    {
        if (serialized is null)
        {
            value = null;
            return false;
        }

        // Check format.
        if (!DateTimeScalarRegex.IsMatch(serialized))
        {
            value = null;
            return false;
        }

        // No "Unknown Local Offset Convention".
        // https://www.graphql-scalars.com/date-time/#no-unknown-local-offset-convention
        if (serialized.EndsWith("-00:00"))
        {
            value = null;
            return false;
        }

        if (DateTimeOffset.TryParse(
                serialized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            value = dt;
            return true;
        }

        value = null;
        return false;
    }
}
