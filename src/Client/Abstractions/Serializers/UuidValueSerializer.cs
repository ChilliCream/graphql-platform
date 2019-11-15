using System;

namespace StrawberryShake.Serializers
{
    public class UuidValueSerializer
        : ValueSerializerBase<Guid, string>
    {
        public UuidValueSerializer()
            : this(WellKnownScalars.Uuid)
        {
        }

        internal UuidValueSerializer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string Name { get; }

        public override ValueKind Kind => ValueKind.String;

        public override object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is Guid guid)
            {
                return guid.ToString("N");
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{ClrType.FullName} was expeceted.");
        }

        public override object? Deserialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string s)
            {
                return Guid.Parse(s);
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{SerializationType.FullName} was expeceted.");
        }
    }
}
