using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    public class LocalTimeType : ScalarType<DateTime, StringValueNode>
    {
        private const string _dateFormat = "HH:mm:ss";
        public LocalTimeType(
            NameString name,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeType"/> class.
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

            if (resultValue is DateTime dt)
            {
                return ParseValue(dt);
            }

            throw new SerializationException(
                Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }

        protected override DateTime ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTime? value))
            {
                return value.Value;
            }

            throw new SerializationException(
                Scalar_Cannot_ParseResult(Name, valueSyntax.GetType()),
                this);
        }

        protected override StringValueNode ParseValue(DateTime runtimeValue) =>
            new(Serialize(runtimeValue));

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is DateTime dt)
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

            if (resultValue is DateTime)
            {
                runtimeValue = resultValue;
                return true;
            }

            runtimeValue = null;
            return false;
        }


        // TODO: Return the time or the whole Date object?
        private static string Serialize(DateTime value) =>
            value.ToLocalTime().ToString(_dateFormat, CultureInfo.InvariantCulture);

        private static bool TryDeserializeFromString(
            string? serialized,
            [NotNullWhen(true)]out DateTime? value)
        {
            if (DateTime.TryParse(
                serialized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime dateTime))
            {
                value = dateTime.Date;
                return true;
            }

            value = null;
            return false;
        }

        public static string Scalar_Cannot_ParseResult(
            string typeName, Type valueType)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(
                    ScalarResources.LocalTimeType_TypeNameEmptyOrNull,
                    nameof(typeName));
            }

            if (valueType is null)
            {
                throw new ArgumentNullException(nameof(valueType));
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                ScalarResources.LocalTimeType_TypeNameEmptyOrNull,
                typeName,
                valueType.FullName);
        }
    }
}
