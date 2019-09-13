using System;
using System.Globalization;
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

            if (serialized is DateTimeOffset)
            {
                value = serialized;
                return true;
            }

            if (serialized is DateTime dt)
            {
                value = new DateTimeOffset(
                    dt.ToUniversalTime(),
                    TimeSpan.Zero);
                return true;
            }

            value = null;
            return false;
        }

        protected override bool TryDeserializeFromString(
            string serialized,
            out object obj)
        {
            if (serialized != null
                && serialized.EndsWith("Z")
                && DateTime.TryParse(
                    serialized,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime zuluTime))
            {
                obj = new DateTimeOffset(
                    zuluTime.ToUniversalTime(),
                    TimeSpan.Zero);
                return true;
            }

            if (DateTimeOffset.TryParse(
                serialized,
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
