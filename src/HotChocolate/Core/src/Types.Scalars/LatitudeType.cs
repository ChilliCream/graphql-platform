using System;
using System.Globalization;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LatitudeType` scalar represents a valid decimal degrees latitude number.
    /// <a>https://en.wikipedia.org/wiki/Latitude</a>
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

        /// <inheritdoc />
        public override IValueNode ParseResult(object? resultValue)
        {
            return resultValue switch
            {
                null => NullValueNode.Default,

                string s when Latitude.IsSexagesimal(s) &&
                              Latitude.TryDeserializeFromString(s, out var value) =>
                    ParseValue(value),

                double d => ParseValue(d),

                _ => throw ThrowHelper.LatitudeType_ParseValue_IsInvalid(this)
            };
        }

        /// <inheritdoc />
        protected override double ParseLiteral(StringValueNode valueSyntax)
        {
            if (Latitude.TryDeserializeFromString(valueSyntax.Value, out var value) &&
                value != null)
            {
                return value.Value;
            }

            throw ThrowHelper.LatitudeType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(double runtimeValue)
        {
            if (runtimeValue is < Latitude._minValue or > Latitude._maxValue)
            {
                return new StringValueNode(
                    Math.Round(
                        runtimeValue,
                        Latitude._maxPrecision,
                        MidpointRounding.AwayFromZero)
                        .ToString(CultureInfo.InvariantCulture));
            }
            throw ThrowHelper.LatitudeType_ParseValue_IsInvalid(this);
        }

        private static class Latitude
        {
            internal const double _minValue = -90.0;
            internal const double _maxValue = 90.0;
            // https://en.wikipedia.org/wiki/Decimal_degrees#Precision
            internal const int _maxPrecision = 8;

            private const string SexagesimalRegex =
                "^([0-9]{1,3})°\\s*([0-9]{1,3}(?:\\.(?:[0-9]{1,}))?)['′]\\s*(([0-9]{1,3}(\\.([0-9]{1,}))" +
                "?)[\"″]\\s*)?([NEOSW]?)$";

            private static readonly Regex _rx = new(
                SexagesimalRegex,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            internal static bool IsSexagesimal(string s)
            {
                return _rx.IsMatch(s);
            }

            internal static bool TryDeserializeFromString(string serialized, out double? value)
            {
                MatchCollection coords = _rx.Matches(serialized);
                if (coords.Count > 0)
                {
                    var degree = double.Parse(coords[0].Groups[1].Value);
                    var minute = double.TryParse(coords[0].Groups[2].Value, out var min) ? min / 60 : 0;
                    var second = double.TryParse(coords[0].Groups[4].Value, out var sec) ? sec / 3600 : 0;
                    var result = Math.Round(degree + minute + second, _maxPrecision, MidpointRounding.AwayFromZero);

                    // Southern and western coordinates must be negative decimals
                    value = coords[0].Groups[7].Value is "W" or "S" ? -result : result;
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
