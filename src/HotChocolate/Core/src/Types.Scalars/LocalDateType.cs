using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `LocalDate` scalar type represents a ISO date string, represented as UTF-8
    /// character sequences YYYY-MM-DD. The scalar follows the specification defined in
    /// <a href="https://tools.ietf.org/html/rfc3339">RFC3339</a>
    /// </summary>
    public class LocalDateType  : ScalarType<DateTime, StringValueNode>
    {
        private const string _localFormat = "yyyy-MM-dd";

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDateType"/> class.
        /// </summary>
        public LocalDateType()
            : this(
                WellKnownScalarTypes.LocalDate,
                ScalarResources.LocalDateType_Description,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDateType"/> class.
        /// </summary>
        public LocalDateType(
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
                DateTime dt => ParseValue(new DateTimeOffset(dt)),
                _ => throw ThrowHelper.LocalDateType_ParseValue_IsInvalid(this)
            };
        }

        protected override DateTime ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTime? value))
            {
                return value.Value;
            }

            throw ThrowHelper.LocalDateType_ParseLiteral_IsInvalid(this);
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
                case DateTimeOffset:
                    runtimeValue = resultValue;
                    return true;
                case DateTime dt:
                    runtimeValue = new DateTimeOffset(
                        dt.ToUniversalTime(),
                        TimeSpan.Zero);
                    return true;
                default:
                    runtimeValue = null;
                    return false;
            }
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
