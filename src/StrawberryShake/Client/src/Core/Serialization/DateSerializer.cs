using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization
{
    public class DateSerializer : ScalarSerializer<string, DateTime>
    {
        private const string _dateFormat = "yyyy-MM-dd";

        public DateSerializer(string typeName = BuiltInTypeNames.Date)
            : base(typeName)
        {
        }

        public override DateTime Parse(string serializedValue)
        {
            if (TryDeserializeFromString(serializedValue, out DateTime? date))
            {
                return date.Value;
            }

            throw ThrowHelper.DateTimeSerializer_InvalidFormat(serializedValue);
        }

        protected override string Format(DateTime runtimeValue)
        {
            return runtimeValue.Date.ToString(_dateFormat, CultureInfo.InvariantCulture);
        }

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
    }
}
