using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LocalTime` scalar type is a local time string (i.e., with no associated timezone)
    /// in 24-hr HH:mm:ss.
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
                ScalarResources.LocalTimeType_Description)
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
            return resultValue switch
            {
                null => NullValueNode.Default,
                string s => new StringValueNode(s),
                DateTimeOffset d => ParseValue(d),
                DateTime dt => ParseValue(dt),
                _ => throw ThrowHelper.LocalTimeType_ParseValue_IsInvalid(this)
            };
        }

        protected override DateTime ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTime? value))
            {
                return value.Value;
            }

            throw ThrowHelper.LocalTimeType_ParseLiteral_IsInvalid(this);
        }

        protected override StringValueNode ParseValue(DateTime runtimeValue)
        {
            return new(Serialize(runtimeValue));
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            switch (runtimeValue)
            {
                case null:
                    resultValue = null;
                    return true;
                case DateTimeOffset dt:
                    resultValue = Serialize(dt);
                    return true;
                case DateTime dt:
                    resultValue = Serialize(dt);
                    return true;
                default:
                    resultValue = null;
                    return false;
            }
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            switch (resultValue)
            {
                case null:
                    runtimeValue = null;
                    return true;
                case string s when TryDeserializeFromString(s, out DateTime? d):
                    runtimeValue = d;
                    return true;
                case DateTimeOffset d:
                    runtimeValue = d.DateTime;
                    return true;
                case DateTime d:
                    runtimeValue = d;
                    return true;
                default:
                    runtimeValue = null;
                    return false;
            }
        }

        private static string Serialize(IFormattable value)
        {
            return value.ToString(_localFormat, CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(
            string? serialized,
            [NotNullWhen(true)] out DateTime? value)
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
