using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LongitudeType` scalar represents a valid decimal degrees longitude number.
    /// <a href="https://en.wikipedia.org/wiki/Longitude">Read More</a>
    /// </summary>
    public class LongitudeType : ScalarType<double, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LongitudeType"/>
        /// </summary>
        public LongitudeType()
            : this(
                WellKnownScalarTypes.Longitude,
                ScalarResources.LongitudeType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LongitudeType"/>
        /// </summary>
        public LongitudeType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(double runtimeValue)
        {
            return runtimeValue is > Longitude.Min and < Longitude.Max;
        }

        /// <inheritdoc />
        public override IValueNode ParseResult(object? resultValue)
        {
            return resultValue switch
            {
                null => NullValueNode.Default,

                string s when Longitude.TryDeserialize(s, out var runtimeValue) => ParseValue(runtimeValue),

                int i => ParseValue(i),

                double d => ParseValue(d),

                _ => throw ThrowHelper.LongitudeType_ParseValue_IsInvalid(this)
            };
        }

        /// <inheritdoc />
        protected override double ParseLiteral(StringValueNode valueSyntax)
        {
            if (Longitude.TryDeserialize(valueSyntax.Value,  out var runtimeValue))
            {
                return runtimeValue.Value;
            }

            throw ThrowHelper.LongitudeType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(double runtimeValue)
        {
            if (Longitude.TrySerialize(runtimeValue, out var s))
            {
                return new StringValueNode(s);
            }

            throw ThrowHelper.LongitudeType_ParseValue_IsInvalid(this);
        }

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            switch (runtimeValue)
            {
                case double d when Longitude.TrySerialize(d, out var serializedDouble):
                    resultValue = serializedDouble;
                    return true;

                case int i when Longitude.TrySerialize(i, out var serializedInt):
                    resultValue = serializedInt;
                    return true;

                default:
                    resultValue = null;
                    return false;
            }
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is string s &&
                Longitude.TryDeserialize(s, out var value) &&
                value is > Longitude.Min and < Longitude.Max)
            {
                runtimeValue = value;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        private static class Longitude
        {
            public const double Min = -180.0;
            public const double Max = 180.0;
            private const int MaxPrecision = 8;

            private const string SexagesimalRegex =
                "^([0-9]{1,3})°\\s*([0-9]{1,3}(?:\\.(?:[0-9]{1,}))?)['′]\\s*(([0-9]{1,3}" +
                "(\\.([0-9]{1,}))?)[\"″]\\s*)?([NEOSW]?)$";

            private static readonly Regex _validationPattern =
                new(SexagesimalRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public static bool TryDeserialize(
                string serialized,
                [NotNullWhen(true)] out double? value)
            {
                MatchCollection coords = _validationPattern.Matches(serialized);
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
                    value = coords[0].Groups[7].Value is "W" or "S" ? -result : result;
                    return true;
                }

                value = null;
                return false;
            }

            public static bool TrySerialize(
                double runtimeValue,
                [NotNullWhen(true)] out string? resultValue)
            {
                if (runtimeValue is > Min and < Max)
                {
                    var degree =  runtimeValue > 0
                        ? Math.Floor(runtimeValue)
                        : Math.Ceiling(runtimeValue);
                    var degreeDecimals = runtimeValue - degree;

                    var minutesWhole = degreeDecimals * 60;
                    var minutes = minutesWhole > 0
                        ? Math.Floor(minutesWhole)
                        : Math.Ceiling(minutesWhole);
                    var minutesDecimal = minutesWhole - minutes;

                    var seconds = Math.Round(
                        minutesDecimal * 60,
                        MaxPrecision,
                        MidpointRounding.AwayFromZero);

                    string serializedLatitude = degree switch
                    {
                        >= 0 and < Max =>
                            $"{degree}° {minutes}' {seconds}\" E",
                        < 0 and > Min =>
                            $"{Math.Abs(degree)}° {Math.Abs(minutes)}' {Math.Abs(seconds)}\" W",
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
}
