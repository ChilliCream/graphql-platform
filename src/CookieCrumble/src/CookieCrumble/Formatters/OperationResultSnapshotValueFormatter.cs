using System.Buffers;
using System.Text.Json;
using HotChocolate.Transport;

namespace CookieCrumble.Formatters;

internal sealed class OperationResultSnapshotValueFormatter : SnapshotValueFormatter<OperationResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, OperationResult value)
    {
        if (value.Data.ValueKind is JsonValueKind.Object)
        {
            snapshot.Append("Data:");
            snapshot.AppendLine();
            snapshot.Append(value.Data.ToString());
        }
        
        if (value.Errors.ValueKind is JsonValueKind.Array)
        {
            snapshot.Append("Errors:");
            snapshot.AppendLine();
            snapshot.Append(value.Errors.ToString());
        }
        
        if (value.Extensions.ValueKind is JsonValueKind.Object)
        {
            snapshot.Append("Extensions:");
            snapshot.AppendLine();
            snapshot.Append(value.Extensions.ToString());
        }
    }
}