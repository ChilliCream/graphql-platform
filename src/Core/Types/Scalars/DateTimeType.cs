using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class DateTimeType
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
    }
}
