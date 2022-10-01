using System.Buffers;
using HotChocolate;
using HotChocolate.Execution;

namespace CookieCrumble.Formatters;

internal sealed class ExecutionResultSnapshotValueFormatter
    : SnapshotValueFormatter<IExecutionResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, IExecutionResult value)
    {
        if (value.Kind is ExecutionResultKind.SingleResult)
        {
            snapshot.Append(value.ToJson());
        }
        else
        {
            FormatStreamAsync(snapshot, (IResponseStream)value).Wait();
        }
    }

    private static async Task FormatStreamAsync(
        IBufferWriter<byte> snapshot,
        IResponseStream stream)
    {
        await foreach (var queryResult in stream.ReadResultsAsync().ConfigureAwait(false))
        {
            snapshot.Append(queryResult.ToJson());
            snapshot.AppendLine();
        }
    }
}
