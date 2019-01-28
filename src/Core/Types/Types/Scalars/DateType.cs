using System;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DateType
        : DateTimeTypeBase
    {
        public DateType()
            : base("Date")
        {
        }

        public override string Description =>
            TypeResources.DateType_Description();

        public override Type ClrType => typeof(DateTime);

        protected override string Serialize(DateTime value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        protected override string Serialize(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        protected override bool TryParseLiteral(
            StringValueNode literal, out object obj)
        {
            if (DateTime.TryParse(
                literal.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out DateTime dateTime))
            {
                obj = dateTime;
                return true;
            }

            obj = null;
            return false;
        }
    }
}
