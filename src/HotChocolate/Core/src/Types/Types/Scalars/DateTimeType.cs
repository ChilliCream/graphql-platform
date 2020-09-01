using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DateTimeType
        : ScalarType<DateTimeOffset, StringValueNode>
    {
        private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        public DateTimeType()
            : base(ScalarNames.DateTime, BindingBehavior.Implicit)
        {
            Description = TypeResources.DateTimeType_Description;
        }

        public DateTimeType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        public DateTimeType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override DateTimeOffset ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTimeOffset? value))
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, valueSyntax.GetType()));
        }

        protected override StringValueNode ParseValue(DateTimeOffset runtimeValue)
        {
            return new StringValueNode(Serialize(runtimeValue));
        }

        public override bool TrySerialize(object runtimeValue, out object resultValue)
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

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
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
            if (value.Offset == TimeSpan.Zero)
            {
                return value.ToString(
                    _utcFormat,
                    CultureInfo.InvariantCulture);
            }

            return value.ToString(
                _localFormat,
                CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(string serialized, out DateTimeOffset? value)
        {
            if (serialized != null
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

            if (DateTimeOffset.TryParse(
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
