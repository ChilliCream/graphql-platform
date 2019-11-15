using System;

namespace StrawberryShake.Serializers
{
    public abstract class ValueSerializerBase<TRuntime, TSerialized>
        : IValueSerializer
    {
        public abstract string Name { get; }

        public abstract ValueKind Kind { get; }

        public Type ClrType => typeof(TRuntime);

        public Type SerializationType => typeof(TSerialized);

        public virtual object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is TRuntime)
            {
                return value;
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{ClrType.FullName} was expeceted.");
        }

        public virtual object? Deserialize(object? serialized)
        {
            if (serialized is null)
            {
                return null;
            }

            if (serialized is TSerialized)
            {
                return serialized;
            }

            throw new ArgumentException(
                "The specified value is of an invalid type. " +
                $"{SerializationType.FullName} was expeceted.");
        }
    }
}
