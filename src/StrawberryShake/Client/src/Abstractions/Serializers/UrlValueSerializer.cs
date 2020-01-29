using System;

namespace StrawberryShake.Serializers
{
    public class UrlValueSerializer
        : ValueSerializerBase<Uri, string>
    {
        public override string Name => WellKnownScalars.Url;

        public override ValueKind Kind => ValueKind.String;

        public override object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is Uri)
            {
                return value.ToString();
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
                return new Uri(s);
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{SerializationType.FullName} was expeceted.");
        }
    }
}
