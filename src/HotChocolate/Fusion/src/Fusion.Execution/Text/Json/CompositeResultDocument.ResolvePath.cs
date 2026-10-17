using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal bool TryResolvePath(CompactPath path, out CompositeResultElement element)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var cursor = Cursor.CreateZero();

        foreach (var segment in path.Segments)
        {
            var logicalCursor = cursor;
            var row = _metaDb.GetValue(ref cursor);

            if (row.TokenType is ElementTokenType.Null)
            {
                element = new CompositeResultElement(this, logicalCursor);
                return true;
            }

            if (segment >= 0)
            {
                if (row.TokenType is not ElementTokenType.StartObject
                    || row.OperationReferenceType is not OperationReferenceType.SelectionSet)
                {
                    element = default;
                    return false;
                }

                var propertyIndex = segment - row.OperationReferenceId - 1;
                var propertyCursor = cursor + (propertyIndex * 2) + 1;
                Debug.Assert(_metaDb.GetElementTokenType(propertyCursor) is ElementTokenType.PropertyName);
                Debug.Assert(_metaDb.Get(propertyCursor).OperationReferenceId == segment);
                cursor = propertyCursor + 1;
            }
            else
            {
                var index = ~segment;

                if (row.TokenType is not ElementTokenType.StartArray
                    || (uint)index >= (uint)row.NumberOfRows)
                {
                    element = default;
                    return false;
                }

                cursor = cursor.AddRows(index + 1);
            }
        }

        element = new CompositeResultElement(this, cursor);
        return true;
    }
}
