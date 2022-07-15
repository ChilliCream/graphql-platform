using System.Buffers;
using HotChocolate;
using HotChocolate.Execution;

namespace Testing;

internal sealed class ExecutionResultSnapshotValueFormatter
    : SnapshotValueFormatter<IExecutionResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, IExecutionResult value)
        => snapshot.Append(value.ToJson());
}
