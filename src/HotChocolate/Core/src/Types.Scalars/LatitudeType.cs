using System;
using System.Net.Http.Headers;
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

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new System.NotImplementedException();
        }

        protected override double ParseLiteral(StringValueNode valueSyntax)
        {
            throw new System.NotImplementedException();
        }

        protected override StringValueNode ParseValue(double runtimeValue)
        {
            throw new System.NotImplementedException();
        }

        private static class Latitude
        {
            private const double MinValue = -90.0;
            private const double MaxValue = 90.0;
            // https://en.wikipedia.org/wiki/Decimal_degrees#Precision
            private const double MaxPrecision = 8;

            private const string SexagesimalRegex =
                "^([0-9]{1,3})°\\s*([0-9]{1,3}(?:\\.(?:[0-9]{1,}))?)['′]\\s*(([0-9]{1,3}(\\.([0-9]{1,}))" +
                "?)[\"″]\\s*)?([NEOSW]?)$";

            private static readonly Regex _rx = new Regex(
                SexagesimalRegex,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private static bool IsSexagesimal(string s)
            {
                return _rx.IsMatch(s);
            }

            private static double TryDeserializeFromString(string s)
            {
                MatchCollection m = _rx.Matches(s);

                var min = double.TryParse(m[2].Value, out var m2) ? m2 / 60 : 0;
                var sec =   double.TryParse(m[4].Value, out var m1) ? m1 / 60 : 0;
                var dec = double.TryParse(m[1].Value, out var m3) ? m3 + min + sec : 0;
                return m[7].Value.Contains("W") || m[7].Value.Contains("S") ? -dec : dec;
            }
        }
    }
}
