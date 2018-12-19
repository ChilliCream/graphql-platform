using System;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DateTimeType
        : DateTimeTypeBase
    {
        public DateTimeType()
            : base("DateTime")
        {
        }

        public override string Description =>
            TypeResources.DateTimeType_Description();

        public override Type ClrType => typeof(DateTimeOffset);

        protected override string Serialize(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
            }

            return value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffzzz");
        }

        protected override string Serialize(DateTimeOffset value)
        {
            if (value.Offset == TimeSpan.Zero)
            {
                return value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
            }

            return value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffzzz");
        }

        protected override bool TryParseLiteral(
            StringValueNode literal,
            out object obj)
        {
            if (literal.Value != null
                && literal.Value.EndsWith("Z")
                && DateTime.TryParse(
                    literal.Value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime zuluTime))
            {
                obj = new DateTimeOffset(zuluTime);
                return true;
            }

            if (DateTimeOffset.TryParse(
                literal.Value,
                out DateTimeOffset dateTime))
            {
                obj = dateTime;
                return true;
            }

            obj = null;
            return false;
        }
    }
}
