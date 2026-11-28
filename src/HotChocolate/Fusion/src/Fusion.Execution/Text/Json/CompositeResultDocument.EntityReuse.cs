namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    /// <summary>
    /// Resolves the objects at <paramref name="cursors"/> to the first rows of objects that a
    /// selection set describes and writes one entry per cursor to <paramref name="objects"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if every cursor refers to an object with a selection set; otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetSelectionSetObjects(ReadOnlySpan<int> cursors, Span<SelectionSetObject> objects)
    {
        objects = objects[..cursors.Length];

        for (var i = 0; i < cursors.Length; i++)
        {
            var startCursor = new Cursor(cursors[i]);
            var row = _metaDb.GetValue(ref startCursor);

            if (row.TokenType is not ElementTokenType.StartObject
                || row.OperationReferenceType is not OperationReferenceType.SelectionSet)
            {
                return false;
            }

            objects[i] = new SelectionSetObject(
                startCursor,
                row.OperationReferenceId,
                (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated);
        }

        return true;
    }

    /// <summary>
    /// The first row of an object that a selection set describes, the id of that selection set
    /// and whether the object is invalidated.
    /// </summary>
    internal readonly struct SelectionSetObject(Cursor startCursor, int selectionSetId, bool isInvalidated)
    {
        public readonly Cursor StartCursor = startCursor;
        public readonly int SelectionSetId = selectionSetId;
        public readonly bool IsInvalidated = isInvalidated;
    }
}
