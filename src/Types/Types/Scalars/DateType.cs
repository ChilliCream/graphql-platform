using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DateType
        : DateTimeTypeBase
    {
        public DateType()
            : base("Date", "ISO-8601 compliant date type.")
        {
        }

        public override Type NativeType => typeof(DateTime);

        protected override string Serialize(DateTime value)
        {
            return value.ToString("yyyy-MM-dd");
        }

        protected override string Serialize(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-dd");
        }

        protected override bool TryParseLiteral(
            StringValueNode literal, out object obj)
        {
            if (DateTime.TryParse(literal.Value, out DateTime dateTime))
            {
                obj = dateTime;
                return true;
            }

            obj = null;
            return false;
        }
    }
}
