using System;

namespace StrawberryShake.Serializers
{
    public class ByteArrayValueSerializer
        : ValueSerializerBase<byte[], string>
    {
        public override string Name => WellKnownScalars.ByteArray;

        public override ValueKind Kind => ValueKind.String;

        public override object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is byte[] b)
            {
                return Convert.ToBase64String(b);
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
                return Convert.FromBase64String(s);
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{SerializationType.FullName} was expeceted.");
        }
    }
}
