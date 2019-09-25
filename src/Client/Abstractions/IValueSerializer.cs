using System;

namespace StrawberryShake
{
    public interface IValueSerializer
    {
        string Name { get; }

        ValueKind Kind { get; }

        Type ClrType { get; }

        Type SerializationType { get; }

        object? Serialize(object? value);

        object? Deserialize(object? serialized);
    }
}
