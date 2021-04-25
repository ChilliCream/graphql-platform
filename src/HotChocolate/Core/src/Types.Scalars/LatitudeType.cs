using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LatitudeType` scalar represents a valid decimal degrees latitude number.
    /// <a href="https://en.wikipedia.org/wiki/Latitude">Read More</a>
    /// </summary>
    public class LatitudeType : ScalarType<double, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LatitudeType"/>
        /// </summary>
        public LatitudeType()
            : this(
                WellKnownScalarTypes.Latitude,
                ScalarResources.LatitudeType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LatitudeType"/>
        /// </summary>
        public LatitudeType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            if(Latitude.TryDeserialize(valueSyntax.Value, out var value))
            {
                return value is > Latitude._min and < Latitude._max;
            }

            return false;
        }

        protected override bool IsInstanceOfType(double valueSyntax)
        {
            return valueSyntax is > Latitude._min and < Latitude._max;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            return resultValue switch
            {
                null => NullValueNode.Default,
                string s => new StringValueNode(s),
                int i => ParseValue(i),
                double d => ParseValue(d),
                _ => throw ThrowHelper.LatitudeType_ParseValue_IsInvalid(this)
            };
        }

        protected override double ParseLiteral(StringValueNode valueSyntax)
        {
            if (Latitude.TryDeserialize(valueSyntax.Value,  out var runtimeValue))
            {
                return runtimeValue.Value;
            }

            throw ThrowHelper.LatitudeType_ParseLiteral_IsInvalid(this);
        }

        protected override StringValueNode ParseValue(double runtimeValue)
        {
            if (Latitude.TrySerialize(runtimeValue, out var value))
            {
                return new StringValueNode(value);
            }

            throw ThrowHelper.LatitudeType_ParseLiteral_IsInvalid(this);
        }

        private static class Latitude
        {
            public const double _min = -90.0;
            public const double _max = 90.0;
            private const int MaxPrecision = 8;

            private const string SexagesimalRegex =
                "^([0-9]{1,3})°\\s*([0-9]{1,3}(?:\\.(?:[0-9]{1,}))?)['′]\\s*(([0-9]{1,3}" +
                "(\\.([0-9]{1,}))?)[\"″]\\s*)?([NEOSW]?)$";

            private static readonly Regex _rx =
                new(SexagesimalRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public static bool TryDeserialize(
                string serialized,
                [NotNullWhen(true)] out double? value)
            {
                MatchCollection coords = _rx.Matches(serialized);
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

            public static bool TrySerialize(double runtimeValue, [NotNullWhen(true)] out string? resultValue)
            {
                if (runtimeValue is > _min and < _max)
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
                        > 0 and < _max =>
                            $"{degree}° {minutes}' {seconds}\" N",
                        < 0 and > _min =>
                            $"{Math.Abs(degree)}° {Math.Abs(minutes)}' {Math.Abs(seconds)}\" S",
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
