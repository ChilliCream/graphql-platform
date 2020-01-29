using System;
using System.Globalization;

namespace StrawberryShake.Serializers
{
    public class DateTimeValueSerializer
        : ValueSerializerBase<DateTimeOffset, string>
    {
        private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        public override string Name => WellKnownScalars.DateTime;

        public override ValueKind Kind => ValueKind.String;

        public override object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is DateTimeOffset d)
            {
                if (d.Offset == TimeSpan.Zero)
                {
                    return d.ToString(
                        _utcFormat,
                        CultureInfo.InvariantCulture);
                }

                return d.ToString(
                    _localFormat,
                    CultureInfo.InvariantCulture);
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{ClrType.FullName} was expeceted.");
        }

        public override object? Deserialize(object? serialized)
        {
            if (serialized is null)
            {
                return null;
            }

            if (serialized is string s)
            {
                if (s.EndsWith("Z")
                    && DateTime.TryParse(
                        s,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal,
                        out DateTime zuluTime))
                {
                    return new DateTimeOffset(
                        zuluTime.ToUniversalTime(),
                        TimeSpan.Zero);
                }

                if (DateTimeOffset.TryParse(s, out DateTimeOffset dateTime))
                {
                    return dateTime;
                }
            }

            throw new ArgumentException(
                "The specified value is of an invalid type or format. " +
                $"{SerializationType.FullName} was expeceted.");
        }
    }
}
