using System.Buffers;
using System.Text.Json;
using HotChocolate.Transport;

namespace CookieCrumble.Formatters;

internal sealed class OperationResultSnapshotValueFormatter : SnapshotValueFormatter<OperationResult>
{
    protected override void Format(IBufferWriter<byte> snapshot, OperationResult value)
    {
        var next = false;
        
        if(value.RequestIndex.HasValue)
        {
            snapshot.Append("RequestIndex: ");
            snapshot.Append(value.RequestIndex.Value.ToString());
            next = true;
        }
        
        if(value.VariableIndex.HasValue)
        {
            snapshot.AppendLine(appendWhenTrue: next);   
            snapshot.Append("VariableIndex: ");
            snapshot.Append(value.VariableIndex.Value.ToString());
            next = true;
        }
        
        if (value.Data.ValueKind is JsonValueKind.Object)
        {
            snapshot.AppendLine(appendWhenTrue: next);
            snapshot.Append("Data: ");
            snapshot.Append(value.Data.ToString());
            next = true;
        }
        
        if (value.Errors.ValueKind is JsonValueKind.Array)
        {
            snapshot.AppendLine(appendWhenTrue: next);
            snapshot.Append("Errors: ");
            snapshot.Append(value.Errors.ToString());
            next = true;
        }
        
        if (value.Extensions.ValueKind is JsonValueKind.Object)
        {
            snapshot.AppendLine(appendWhenTrue: next);
            snapshot.Append("Extensions: ");
            snapshot.Append(value.Extensions.ToString());
        }
    }
}