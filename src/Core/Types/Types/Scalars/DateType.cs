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

        public DateType() : base(ScalarNames.Date)
        {
            Description = TypeResources.DateType_Description;
        }

        protected override DateTime ParseLiteral(StringValueNode literal)
        {
            if (TryDeserializeFromString(literal.Value, out DateTime? value))
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(DateTime value)
        {
            return new StringValueNode(Serialize(value));
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is DateTime dt)
            {
                serialized = Serialize(dt);
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s && TryDeserializeFromString(s, out DateTime? d))
            {
                value = d;
                return true;
            }

            if (serialized is DateTimeOffset dt)
            {
                value = dt.UtcDateTime;
                return true;
            }

            if (serialized is DateTime)
            {
                value = serialized;
                return true;
            }

            value = null;
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
