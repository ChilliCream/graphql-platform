using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DateTimeType
        : DateTimeTypeBase
    {
        private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        public DateTimeType()
            : base("DateTime")
        {
            Description = TypeResources.DateTimeType_Description;
        }

        public override Type ClrType => typeof(DateTimeOffset);

        protected override string Serialize(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value.ToString(
                    _utcFormat,
                    CultureInfo.InvariantCulture);
            }

            return value.ToString(
                _localFormat,
                CultureInfo.InvariantCulture);
        }

        protected override string Serialize(DateTimeOffset value)
        {
            if (value.Offset == TimeSpan.Zero)
            {
                return value.ToString(
                    _utcFormat,
                    CultureInfo.InvariantCulture);
            }

            return value.ToString(
                _localFormat,
                CultureInfo.InvariantCulture);
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
