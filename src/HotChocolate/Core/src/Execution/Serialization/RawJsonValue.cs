using System;

namespace HotChocolate.Execution.Serialization;

internal readonly struct RawJsonValue
{
    public RawJsonValue(ReadOnlyMemory<byte> value)
    {
        Value = value;
    }

    public ReadOnlyMemory<byte> Value { get; }
}
