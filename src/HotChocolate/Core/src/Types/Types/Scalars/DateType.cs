using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DateType
        : ScalarType<DateTime, StringValueNode>
    {
        private const string _dateFormat = "yyyy-MM-dd";

        public DateType()
            : base(ScalarNames.Date, BindingBehavior.Explicit)
        {
            Description = TypeResources.DateType_Description;
        }

        public DateType(NameString name)
            : base(name, BindingBehavior.Explicit)
        {
        }

        public DateType(NameString name, string description)
            : base(name, BindingBehavior.Explicit)
        {
            Description = description;
        }

        protected override DateTime ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryDeserializeFromString(valueSyntax.Value, out DateTime? value))
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, valueSyntax.GetType()));
        }

        protected override StringValueNode ParseValue(DateTime runtimeValue)
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

            if (runtimeValue is DateTime dt)
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

            if (resultValue is string s && TryDeserializeFromString(s, out DateTime? d))
            {
                runtimeValue = d;
                return true;
            }

            if (resultValue is DateTimeOffset dt)
            {
                runtimeValue = dt.UtcDateTime;
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

        private static string Serialize(DateTime value)
        {
            return value.Date.ToString(
                _dateFormat,
                CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(string serialized, out DateTime? value)
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
    }
}
