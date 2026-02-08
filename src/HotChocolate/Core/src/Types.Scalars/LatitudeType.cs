using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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
    protected override double OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (Latitude.TryDeserialize(valueLiteral.Value, out var runtimeValue))
        {
            return runtimeValue.Value;
        }

        throw ThrowHelper.LatitudeType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override double OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (Latitude.TryDeserialize(inputValue.GetString()!, out var runtimeValue))
        {
            return runtimeValue.Value;
        }

        throw ThrowHelper.LatitudeType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(double runtimeValue, ResultElement resultValue)
    {
        if (Latitude.TrySerialize(runtimeValue, out var serialized))
        {
            resultValue.SetStringValue(serialized);
            return;
        }

        throw ThrowHelper.LatitudeType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(double runtimeValue)
    {
        if (Latitude.TrySerialize(runtimeValue, out var serialized))
        {
            return new StringValueNode(serialized);
        }

        throw ThrowHelper.LatitudeType_InvalidFormat(this);
    }

    private static class Latitude
    {
        private const double Min = -90.0;
        private const double Max = 90.0;
        private const int MaxPrecision = 8;

        private const string SexagesimalRegex =
            @"^([0-9]{1,3})°\s*([0-9]{1,3}(?:\.(?:[0-9]{1,}))?)['′]\s*(([0-9]{1,3}"
            + @"(\.([0-9]{1,}))?)[""″]\s*)?([NEOSW]?)\z";

        private static readonly Regex s_validationPattern =
            new(SexagesimalRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsValid(double value) => value is > Min and < Max;

        public static bool TryDeserialize(
            string serialized,
            [NotNullWhen(true)] out double? value)
        {
            var coords = s_validationPattern.Matches(serialized);
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
                    Round(minutesDecimal * 60, MaxPrecision, MidpointRounding.AwayFromZero);

                var serializedLatitude = degree switch
                {
                    >= 0 and < Max => $"{degree}° {minutes}' {seconds}\" N",
                    < 0 and > Min => $"{Abs(degree)}° {Abs(minutes)}' {Abs(seconds)}\" S",
                    _ => $"{degree}° {minutes}' {seconds}\""
                };

                resultValue = serializedLatitude;
                return true;
            }

            resultValue = null;
            return false;
        }
    }
}
