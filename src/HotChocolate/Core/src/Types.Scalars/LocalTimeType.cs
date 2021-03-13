using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types.Scalars
{
    public class LocalTimeType
    {
        /// <summary>
        /// The `LocalTime` scalar type represents a local time string in 24-hr HH:mm[:ss[.SSS]] format.
        /// </summary>
        public class DateTimeType : ScalarType<DateTimeOffset, StringValueNode>
        {
            private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

            /// <summary>
            /// Initializes a new instance of the <see cref="Types.DateTimeType"/> class.
            /// </summary>
            public DateTimeType()
                : this(
                    WellKnownScalarTypes.MacAddress,
                    ScalarResources.LocalTimeType_Description,
                    BindingBehavior.Implicit)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Types.DateTimeType"/> class.
            /// </summary>
            public DateTimeType(
                NameString name,
                string? description = null,
                BindingBehavior bind = BindingBehavior.Explicit)
                : base(name, bind)
            {
                Description = description;
            }

            protected override DateTimeOffset ParseLiteral(StringValueNode valueSyntax)
            {
                if (TryDeserializeFromString(valueSyntax.Value, out DateTimeOffset? value))
                {
                    return value.Value;
                }

                throw new SerializationException(
                    ThrowHelper.LocalTimeType_ParseLiteral_IsInvalid(this);
            }

            protected override StringValueNode ParseValue(DateTimeOffset runtimeValue)
            {
                return new(Serialize(runtimeValue));
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
                    return ParseValue(new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero));
                }

                throw new SerializationException(
                    TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                    this);
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

                if (resultValue is string s && TryDeserializeFromString(s, out DateTimeOffset? d))
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
                [NotNullWhen(true)] out DateTimeOffset? value)
            {
                if (serialized is not null
                    && serialized.EndsWith("Z")
                    && DateTime.TryParse(
                        serialized,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal,
                        out DateTime zuluTime))
                {
                    value = new DateTimeOffset(
                        zuluTime.ToUniversalTime(),
                        TimeSpan.Zero);
                    return true;
                }

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
}
