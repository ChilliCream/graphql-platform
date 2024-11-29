using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using static System.Math;

namespace HotChocolate.Types;

/// <summary>
/// The `LatitudeType` scalar represents a valid decimal degrees latitude number.
/// <a href="https://en.wikipedia.org/wiki/Latitude">Read More</a>
/// </summary>
public class LatitudeType : ScalarType<double, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="LatitudeType"/>
    /// </summary>
    public LatitudeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LatitudeType"/>
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LatitudeType()
        : this(
            WellKnownScalarTypes.Latitude,
            ScalarResources.LatitudeType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(double runtimeValue) =>
        Latitude.IsValid(runtimeValue);

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,

            string s when Latitude.TryDeserialize(s, out var runtimeValue) =>
                ParseValue(runtimeValue),

            int i => ParseValue(i),

            double d => ParseValue(d),

            _ => throw ThrowHelper.LatitudeType_ParseValue_IsInvalid(this),
        };
    }

    /// <inheritdoc />
    protected override double ParseLiteral(StringValueNode valueSyntax)
    {
        if (Latitude.TryDeserialize(valueSyntax.Value, out var runtimeValue))
        {
            return runtimeValue.Value;
        }

        throw ThrowHelper.LatitudeType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override StringValueNode ParseValue(double runtimeValue)
    {
        if (Latitude.TrySerialize(runtimeValue, out var s))
        {
            return new StringValueNode(s);
        }

        throw ThrowHelper.LatitudeType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        switch (runtimeValue)
        {
            case double d when Latitude.TrySerialize(d, out var serializedDouble):
                resultValue = serializedDouble;
                return true;

            case int i when Latitude.TrySerialize(i, out var serializedInt):
                resultValue = serializedInt;
                return true;

            default:
                resultValue = null;
                return false;
        }
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is string s &&
            Latitude.TryDeserialize(s, out var value))
        {
            runtimeValue = value;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    private static class Latitude
    {
        private const double _min = -90.0;
        private const double _max = 90.0;
        private const int _maxPrecision = 8;

        private const string _sexagesimalRegex =
            "^([0-9]{1,3})°\\s*([0-9]{1,3}(?:\\.(?:[0-9]{1,}))?)['′]\\s*(([0-9]{1,3}" +
            "(\\.([0-9]{1,}))?)[\"″]\\s*)?([NEOSW]?)$";

        private static readonly Regex _validationPattern =
            new(_sexagesimalRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsValid(double value) => value is > _min and < _max;

        public static bool TryDeserialize(
            string serialized,
            [NotNullWhen(true)] out double? value)
        {
            var coords = _validationPattern.Matches(serialized);
            if (coords.Count > 0)
            {
                var minute = double.TryParse(coords[0].Groups[2].Value, out var min)
                    ? min / 60
                    : 0;
                var second = double.TryParse(coords[0].Groups[4].Value, out var sec)
                    ? sec / 3600
                    : 0;
                var degree = double.Parse(coords[0].Groups[1].Value);
                var result = degree + minute + second;

                // Southern and western coordinates must be negative decimals
                var deserialized = coords[0].Groups[7].Value is "W" or "S" ? -result : result;

                value = deserialized;
                return IsValid(deserialized);
            }

            value = null;
            return false;
        }

        public static bool TrySerialize(
            double runtimeValue,
            [NotNullWhen(true)] out string? resultValue)
        {
            if (IsValid(runtimeValue))
            {
                var degree = runtimeValue >= 0
                    ? Floor(runtimeValue)
                    : Ceiling(runtimeValue);
                var degreeDecimals = runtimeValue - degree;

                var minutesWhole = degreeDecimals * 60;
                var minutes = minutesWhole >= 0
                    ? Floor(minutesWhole)
                    : Ceiling(minutesWhole);
                var minutesDecimal = minutesWhole - minutes;

                var seconds =
                    Round(minutesDecimal * 60, _maxPrecision, MidpointRounding.AwayFromZero);

                var serializedLatitude = degree switch
                {
                    >= 0 and < _max => $"{degree}° {minutes}' {seconds}\" N",
                    < 0 and > _min => $"{Abs(degree)}° {Abs(minutes)}' {Abs(seconds)}\" S",
                    _ => $"{degree}° {minutes}' {seconds}\"",
                };

                resultValue = serializedLatitude;
                return true;
            }

            resultValue = null;
            return false;
        }
    }
}
