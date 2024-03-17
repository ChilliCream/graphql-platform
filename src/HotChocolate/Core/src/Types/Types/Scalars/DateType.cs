using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public class DateType : ScalarType<DateTime, StringValueNode>
{
    private const string _dateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
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
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DateType() : this(ScalarNames.Date, TypeResources.DateType_Description)
    {
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

    protected override StringValueNode ParseValue(DateTime runtimeValue) =>
        new(Serialize(runtimeValue));

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

        if (resultValue is DateTimeOffset o)
        {
            return ParseValue(o.DateTime);
        }

        if (resultValue is DateTime dt)
        {
            return ParseValue(dt);
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

        if (runtimeValue is DateTime dt)
        {
            resultValue = Serialize(dt);
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

        if (resultValue is DateTimeOffset dt)
        {
            runtimeValue = dt.UtcDateTime;
            return true;
        }

        if (resultValue is DateTime)
        {
            runtimeValue = resultValue;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    private static string Serialize(DateTime value) =>
        value.Date.ToString(_dateFormat, CultureInfo.InvariantCulture);

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateTime? value)
    {
        if (DateTime.TryParse(
           serialized,
           CultureInfo.InvariantCulture,
           DateTimeStyles.None,
           out var dateTime))
        {
            value = dateTime.Date;
            return true;
        }

        value = null;
        return false;
    }
}
