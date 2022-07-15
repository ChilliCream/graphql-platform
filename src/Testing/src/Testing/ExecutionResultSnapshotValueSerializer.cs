using System.Buffers;
using HotChocolate;
using HotChocolate.Execution;

namespace Testing;

internal sealed class ExecutionResultSnapshotValueSerializer : ISnapshotValueSerializer
{
    public bool CanHandle(object? value)
        => value is IExecutionResult;

    public void Serialize(IBufferWriter<byte> snapshot, object? value)
    {
        if(value is IExecutionResult result)
        {
            snapshot.Append(result.ToJson());
        }
    }
}
