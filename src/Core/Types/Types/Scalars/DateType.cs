using System;
using System.Globalization;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DateType
        : DateTimeTypeBase
    {
        private const string _dateFormat = "yyyy-MM-dd";

        public DateType()
            : base("Date")
        {
            Description = TypeResources.DateType_Description;
        }

        public override Type ClrType => typeof(DateTime);

        protected override string Serialize(DateTime value)
        {
            return value.ToString(_dateFormat, CultureInfo.InvariantCulture);
        }

        protected override string Serialize(DateTimeOffset value)
        {
            return value.ToString(_dateFormat, CultureInfo.InvariantCulture);
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s
                && TryDeserializeFromString(s, out object d))
            {
                value = d;
                return true;
            }

            if (serialized is DateTime)
            {
                value = serialized;
                return true;
            }

            if (serialized is DateTimeOffset dto)
            {
                value = dto.UtcDateTime;
                return true;
            }

            value = null;
            return false;
        }

        protected override bool TryDeserializeFromString(
            string serialized, out object obj)
        {
            if (DateTime.TryParse(
                serialized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime dateTime))
            {
                obj = dateTime.Date;
                return true;
            }

            obj = null;
            return false;
        }
    }
}
