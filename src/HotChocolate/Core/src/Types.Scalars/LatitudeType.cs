using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LatitudeType` scalar type represents a valid decimal degrees latitude number.
    /// <a>https://en.wikipedia.org/wiki/Latitude</a>
    /// </summary>
    public class LatitudeType : ScalarType<double, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LatitudeType"/> class.
        /// </summary>
        public LatitudeType()
            : this(
                WellKnownScalarTypes.Latitude,
                ScalarResources.LatitudeType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatitudeType"/> class.
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
            // Minimum latitude
            private static readonly double _minLat = -90.0;

            // Maximum latitude
            private static readonly double _maxLat = 90.0;

            // See https://en.wikipedia.org/wiki/Decimal_degrees#Precision
            private static readonly int _maxPrecision = 8;
        }
    }
}
