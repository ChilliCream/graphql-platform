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
    public class LocalTimeType : ScalarType<DateTime, StringValueNode>
    {
        private const string _localFormat = "HH:mm:ss";

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
                return ParseValue(new DateTimeOffset(dt));
            }

            throw new SerializationException(ScalarResources.LocalTimeType_IsInvalid_ParseValue, this);
        }

        protected override DateTime ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTime? value))
            {
                return value.Value;
            }

            throw new SerializationException(ScalarResources.LocalTimeType_IsInvalid_ParseLiteral, this);
        }

        protected override StringValueNode ParseValue(DateTime runtimeValue)
        {
            return new(Serialize(runtimeValue));
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

            if (resultValue is string s && TryDeserializeFromString(s, out DateTime? d))
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
            return value.ToString(
                _localFormat,
                CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(
            string? serialized,
            [NotNullWhen(true)]out DateTime? value)
        {
            if (serialized is not null
                && DateTime.TryParse(
                    serialized,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime dt))
            {
                value = dt;
                return true;
            }

            value = null;
            return false;
        }
    }
}
