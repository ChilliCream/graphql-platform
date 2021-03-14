using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `LocalTime` scalar type is a local time string (i.e., with no associated timezone)
    /// in 24-hr HH:mm[:ss[.SSS]].
    /// </summary>
    public class LocalTimeType : ScalarType<DateTimeOffset, StringValueNode>
    {
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
        /// </summary>
        public LocalTimeType()
            : this(
                WellKnownScalarTypes.LocalTime,
                ScalarResources.LocalTimeType_Description,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
        /// </summary>
        public LocalTimeType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        protected override DateTimeOffset ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTimeOffset? value))
            {
                return value.Value;
            }

            throw new SerializationException(ScalarResources.LocalTimeType_IsInvalid_ParseLiteral, this);
        }

        protected override StringValueNode ParseValue(DateTimeOffset runtimeValue)
        {
            return new(Serialize(runtimeValue));
        }

        private static string Serialize(DateTimeOffset value)
        {
            return value.ToString(
                _localFormat,
                CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(
            string? serialized,
            [NotNullWhen(true)]out DateTimeOffset? value)
        {
            if (serialized is not null
                && DateTimeOffset.TryParse(
                    serialized,
                    out DateTimeOffset dt))
            {
                value = dt;
                return true;
            }

            value = null;
            return false;
        }
    }
}
