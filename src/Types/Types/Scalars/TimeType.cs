using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class TimeType
        : DateTimeTypeBase
    {
        private bool _withTimeZone;

        public TimeType(bool withTimeZone)
            : base("Time", "ISO-8601 compliant time type.")
        {
            _withTimeZone = withTimeZone;
        }

        public TimeType()
            : this(false)
        {
        }

        public override Type NativeType => typeof(TimeSpan);

        protected override bool TrySerialize(
            object value,
            out string serializedValue)
        {
            if (value is TimeSpan time)
            {
                serializedValue = Serialize(time);
                return true;
            }

            return base.TrySerialize(value, out serializedValue);
        }

        private string Serialize(TimeSpan value)
        {
            DateTimeOffset time = new DateTimeOffset(
                1, 1, 1, 0, 0, 0,
                new TimeSpan(0, 0, 0));
            time = time.Add(value);

            if (_withTimeZone)
            {
                return time.ToString("HH\\:mm\\:sszzz");
            }

            return time.ToString("HH\\:mm\\:ss");
        }

        protected override string Serialize(DateTime value)
        {
            if (_withTimeZone)
            {
                return value.ToString("HH\\:mm\\:sszzz");
            }

            return value.ToString("HH\\:mm\\:ss");
        }

        protected override string Serialize(DateTimeOffset value)
        {
            if (_withTimeZone)
            {
                return value.ToString("HH\\:mm\\:sszzz");
            }
            return value.ToString("HH\\:mm\\:ss");
        }

        protected override bool TryParseLiteral(
            StringValueNode literal, out object obj)
        {
            if (DateTimeOffset.TryParse(
                literal.Value,
                out DateTimeOffset dateTime))
            {
                if (dateTime.Offset == TimeSpan.Zero)
                {
                    obj = dateTime.TimeOfDay;
                }
                else
                {
                    obj = dateTime.ToUniversalTime().TimeOfDay;
                }

                return true;
            }

            obj = null;
            return false;
        }
    }
}
