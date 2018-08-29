using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class TimeType
        : DateTimeTypeBase
    {
        public TimeType()
            : base("Time", "ISO-8601 compliant time type.")
        {
        }

        public override Type NativeType => typeof(DateTime);

        protected override string Serialize(DateTime value)
        {
            return value.ToString("HH\\:mm\\:ss");
        }

        protected override string Serialize(DateTimeOffset value)
        {
            return value.ToString("HH\\:mm\\:ss");
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
