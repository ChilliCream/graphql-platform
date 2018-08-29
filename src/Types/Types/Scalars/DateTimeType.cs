using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DateTimeType
        : DateTimeTypeBase
    {
        public DateTimeType()
            : base("DateTime", "ISO-8601 compliant date time type.")
        {
        }

        public override Type NativeType => typeof(DateTimeOffset);

        protected override string Serialize(DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
        }

        protected override string Serialize(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
        }

        protected override bool TryParseLiteral(
            StringValueNode literal, out object obj)
        {
            if (DateTimeOffset.TryParse(literal.Value, out DateTimeOffset dateTime))
            {
                obj = dateTime;
                return true;
            }

            obj = null;
            return false;
        }
    }
}
