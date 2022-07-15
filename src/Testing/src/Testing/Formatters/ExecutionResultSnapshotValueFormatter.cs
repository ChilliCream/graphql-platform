using System.Buffers;
using HotChocolate;
using HotChocolate.Execution;

namespace CookieCrumble.Formatters;

internal sealed class ExecutionResultSnapshotValueFormatter
    : SnapshotValueFormatter<IExecutionResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, IExecutionResult value)
        => snapshot.Append(value.ToJson());
}
