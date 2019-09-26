using System;
using System.Globalization;

namespace StrawberryShake.Serializers
{
    public class DateValueSerializer
        : ValueSerializerBase<DateTime, string>
    {
        private const string _dateFormat = "yyyy-MM-dd";

        public override string Name => WellKnownScalars.Date;

        public override ValueKind Kind => ValueKind.String;

        public override object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is DateTime d)
            {
                return d.ToString(_dateFormat, CultureInfo.InvariantCulture);
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

            if (serialized is string s
                && DateTime.TryParse(
                    s,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime dateTime))
            {
                return dateTime.Date;
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{SerializationType.FullName} was expeceted.");
        }
    }
}
