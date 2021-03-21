using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `UtcOffset` scalar type represents a value of format `±hh:mm`.
    /// </summary>
    public class UtcOffsetType : ScalarType<TimeSpan, StringValueNode>
    {
        private const string _utcFormat = "HH\\:mm";
        /// <summary>
        /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
        /// </summary>
        public UtcOffsetType()
            : this(
                WellKnownScalarTypes.UtcOffset,
                ScalarResources.UtcOffsetType_Description,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
        /// </summary>
        public UtcOffsetType(
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
                string s when TryDeserializeFromString(s, out TimeSpan? timeSpan) => ParseValue(timeSpan),
                TimeSpan ts => ParseValue(ts),
                _ => throw ThrowHelper.UtcOffset_ParseValue_IsInvalid(this)
            };
        }

        protected override TimeSpan ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out TimeSpan? value))
            {
                return value.Value;
            }

            throw ThrowHelper.UtcOffset_ParseLiteral_IsInvalid(this);
        }

        protected override StringValueNode ParseValue(TimeSpan runtimeValue)
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
                case TimeSpan timeSpan:
                    resultValue = Serialize(timeSpan);
                    return true;
                default:
                    resultValue = null;
                    return false;
            }
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s &&
                TryDeserializeFromString(s, out TimeSpan? timeSpan))
            {
                runtimeValue = timeSpan;
                return true;
            }

            if (resultValue is TimeSpan ts)
            {
                runtimeValue = ts;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        private static string Serialize(TimeSpan value)
        {
            return value.ToString(_utcFormat, CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(
            string? serialized,
            [NotNullWhen(true)]out TimeSpan? value)
        {
            if (serialized is not null
                && TimeSpan.TryParse(
                    serialized,
                    out TimeSpan ts))
            {
                value = ts;
                return true;
            }

            value = null;
            return false;
        }
    }
}
