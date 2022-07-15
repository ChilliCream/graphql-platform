using System.Buffers;

namespace Testing;

public interface ISnapshotValueSerializer
{
    bool CanHandle(object? value);

    void Serialize(IBufferWriter<byte> snapshot, object? value);
}
