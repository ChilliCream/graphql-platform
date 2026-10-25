using System.Runtime.InteropServices;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;
using DbRow = HotChocolate.Fusion.Text.Json.CompositeResultDocument.DbRow;
using ElementFlags = HotChocolate.Fusion.Text.Json.CompositeResultDocument.ElementFlags;
using OperationReferenceType = HotChocolate.Fusion.Text.Json.CompositeResultDocument.OperationReferenceType;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The prepared result row block for one object instance of a selection set: StartObject, one
/// PropertyName and value row per selection, EndObject. The rows carry only selection-invariant
/// state, so one template serves every request that executes the declaring operation.
/// Per-instance state (parent pointers and per-request exclusion) is stamped when the block is
/// applied to a result document.
/// </summary>
internal readonly struct ObjectTemplate
{
    private readonly byte[] _rows;
    private readonly int[] _conditionalSelections;

    private ObjectTemplate(byte[] rows, int[] conditionalSelections)
    {
        _rows = rows;
        _conditionalSelections = conditionalSelections;
    }

    /// <summary>
    /// Gets the prepared row block.
    /// </summary>
    public ReadOnlySpan<byte> Rows => _rows;

    /// <summary>
    /// Gets the indices of the selections whose exclusion depends on the request's
    /// skip/include or defer state. The canonical rows never carry
    /// <see cref="ElementFlags.IsExcluded"/>; the flag is applied per
    /// request for these selections only.
    /// </summary>
    public ReadOnlySpan<int> ConditionalSelections => _conditionalSelections;

    public static ObjectTemplate Create(SelectionSet selectionSet)
    {
        var selections = selectionSet.Selections;
        var rows = new byte[((selections.Length * 2) + 2) * DbRow.Size];
        var conditionalCount = 0;

        foreach (var selection in selections)
        {
            if (selection.IsConditional || selection.CanBeDeferred)
            {
                conditionalCount++;
            }
        }

        var conditionalSelections = conditionalCount == 0 ? [] : new int[conditionalCount];
        conditionalCount = 0;

        var startObjectRow = new DbRow(
            ElementTokenType.StartObject,
            sizeOrLength: selections.Length,
            operationReferenceId: selectionSet.Id,
            operationReferenceType: OperationReferenceType.SelectionSet,
            numberOfRows: (selections.Length * 2) + 1);
        MemoryMarshal.Write(rows, in startObjectRow);
        var offset = DbRow.Size;

        for (var i = 0; i < selections.Length; i++)
        {
            var selection = selections[i];

            if (selection.IsConditional || selection.CanBeDeferred)
            {
                conditionalSelections[conditionalCount++] = i;
            }

            var propertyRow = new DbRow(
                ElementTokenType.PropertyName,
                operationReferenceId: selection.Id,
                operationReferenceType: OperationReferenceType.Selection,
                flags: GetPropertyFlags(selection));
            MemoryMarshal.Write(rows.AsSpan(offset), in propertyRow);
            offset += DbRow.Size;

            // __typename resolves to the concrete type of this selection set. Synthesizing
            // it here means payloads that never echo __typename (such as broker event
            // streams) still report it, while a subgraph that does echo it simply overwrites
            // the slot with the identical value. The value row stores the selection-set id
            // as an inline string reference, so the interned type name is resolved (and
            // quoted) lazily when read.
            var valueRow =
                selection.Field.IsIntrospectionField
                    && selection.Field.Name == IntrospectionFieldNames.TypeName
                    ? new DbRow(ElementTokenType.String, location: selectionSet.Id)
                    : new DbRow(ElementTokenType.None);
            MemoryMarshal.Write(rows.AsSpan(offset), in valueRow);
            offset += DbRow.Size;
        }

        var endObjectRow = new DbRow(ElementTokenType.EndObject);
        MemoryMarshal.Write(rows.AsSpan(offset), in endObjectRow);

        return new ObjectTemplate(rows, conditionalSelections);
    }

    /// <summary>
    /// Computes the selection-invariant flags of a property row. Skip/include and defer
    /// state is per request and is never part of the canonical rows.
    /// </summary>
    private static ElementFlags GetPropertyFlags(Selection selection)
    {
        var flags = ElementFlags.None;

        if (selection.IsInternal)
        {
            flags = ElementFlags.IsInternal;
        }

        if (selection.Type.Kind is not TypeKind.NonNull)
        {
            flags |= ElementFlags.IsNullable;
        }

        if (selection.IsEnumValue)
        {
            flags |= ElementFlags.IsEnumValue;
        }

        return flags;
    }
}
