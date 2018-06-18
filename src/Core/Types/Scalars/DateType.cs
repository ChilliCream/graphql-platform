using System;
using System.Collections.Immutable;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class DateType
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
    }
}
