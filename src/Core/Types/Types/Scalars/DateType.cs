using System;
using System.Globalization;
using HotChocolate.Language;
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

        protected override bool TryParseLiteral(
            StringValueNode literal, out object obj)
        {
            if (DateTime.TryParse(
                literal.Value,
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
