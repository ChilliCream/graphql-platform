using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;
using Cursor = HotChocolate.Fusion.Text.Json.SourceResultDocument.Cursor;
using DbRow = HotChocolate.Fusion.Text.Json.SourceResultDocument.DbRow;
using SequentialReader = HotChocolate.Fusion.Text.Json.SourceResultDocument.MetaDb.SequentialReader;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class ValueCompletion
{
    /// <summary>
    /// Completes the properties of an error-free source object with the merge program of its
    /// target selection set. Every property the program does not predict, or whose value is
    /// not a leaf, an object or a list the program covers, is completed by the generic path.
    /// </summary>
    private bool BuildResultWithProgram(
        SourceResultElement source,
        DbRow sourceRow,
        CompositeResultElement target,
        CompositeObjectContext objectContext,
        MergeProgram program)
    {
        var document = source._parent;
        var reader = document.CreateSequentialReader(source._cursor + 1);
        var propertyCount = sourceRow.SizeOrLength;
        var next = 0;

        for (var i = 0; i < propertyCount; i++)
        {
            var nameRow = reader.Read();
            var valueCursor = reader.Cursor;
            var valueRow = reader.Read();
            CompositeResultElement resultField;
            bool completed;

            if (TryMatchEntry(program, document, nameRow, ref next, out var index))
            {
                ref readonly var entry = ref program[index];

                if (entry.RowOffset == 0)
                {
                    SkipValue(ref reader, valueRow);
                    continue;
                }

                resultField = objectContext.GetPropertyAt(entry.RowOffset);
                completed = TryCompleteProgramValue(document, ref reader, valueCursor, valueRow, in entry, resultField, depth: 0);
            }
            else
            {
                if (!objectContext.TryGetProperty(GetName(document, nameRow), out resultField, out var selection))
                {
                    SkipValue(ref reader, valueRow);
                    continue;
                }

                completed = TryCompleteUnprogrammedValue(document, valueCursor, valueRow, program, selection, resultField, depth: 0);
                SkipValue(ref reader, valueRow);
            }

            if (!completed)
            {
                switch (_errorHandlingMode)
                {
                    case ErrorHandlingMode.Propagate:
                        var didPropagateToRoot = PropagateNullValues(resultField);
                        if (didPropagateToRoot)
                        {
                            return false;
                        }

                        return ApplyPocketedErrors(target);
                }
            }
        }

        return ApplyPocketedErrors(target);
    }

    /// <summary>
    /// Completes a property the program does not list the way the generic completion does.
    /// </summary>
    private bool TryCompleteUnprogrammedValue(
        SourceResultDocument document,
        Cursor valueCursor,
        DbRow valueRow,
        MergeProgram program,
        Selection selection,
        CompositeResultElement target,
        int depth)
    {
        var snapshot = new SourceResultElementSnapshot(document, valueCursor, valueRow);

        switch (valueRow.TokenType)
        {
            case JsonTokenType.String when selection.IsEnumValue:
                CompleteEnumValue(snapshot, target, Unsafe.As<FusionEnumTypeDefinition>(selection.NamedType));
                return true;

            case JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False:
                target.SetLeafValue(snapshot);
                return true;

            default:
                return TryCompleteValue(
                    snapshot,
                    target,
                    errorTrie: null,
                    selection,
                    depth,
                    program.GetChildResultSelectionSet(selection));
        }
    }

    private bool TryCompleteProgramValue(
        SourceResultDocument document,
        ref SequentialReader reader,
        Cursor valueCursor,
        DbRow valueRow,
        in MergeProgramEntry entry,
        CompositeResultElement target,
        int depth)
    {
        var selection = entry.Selection!;

        switch (valueRow.TokenType)
        {
            case JsonTokenType.String:
                if (selection.IsEnumValue)
                {
                    CompleteEnumValue(
                        new SourceResultElementSnapshot(document, valueCursor, valueRow),
                        target,
                        Unsafe.As<FusionEnumTypeDefinition>(selection.NamedType));
                    return true;
                }

                target.SetLeafValue(new SourceResultElementSnapshot(document, valueCursor, valueRow));
                return true;

            case JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False:
                target.SetLeafValue(new SourceResultElementSnapshot(document, valueCursor, valueRow));
                return true;

            case JsonTokenType.StartObject when entry.Op is MergeOp.Object:
                return TryCompleteObjectWithProgram(document, ref reader, valueCursor, valueRow, in entry, target, depth);

            case JsonTokenType.StartArray when entry.Op is not MergeOp.Generic:
                return TryCompleteListWithProgram(document, ref reader, valueRow, in entry, target, depth);

            default:
                var completed = TryCompleteValue(
                    new SourceResultElementSnapshot(document, valueCursor, valueRow),
                    target,
                    errorTrie: null,
                    selection,
                    depth,
                    entry.ChildResultSelectionSet);
                SkipValue(ref reader, valueRow);
                return completed;
        }
    }

    private bool TryCompleteObjectWithProgram(
        SourceResultDocument document,
        ref SequentialReader reader,
        Cursor objectCursor,
        DbRow objectRow,
        in MergeProgramEntry entry,
        CompositeResultElement target,
        int depth)
    {
        var program = entry.Child!;

        if (!target.TryGetObjectContext(out var objectContext))
        {
            target.SetObjectValue(entry.ChildSelectionSet!, out objectContext);
        }
        else if (objectContext.SelectionSet?.Id != program.SelectionSet.Id)
        {
            var selection = entry.Selection!;
            var completed = TryCompleteObjectValue(
                new SourceResultElementSnapshot(document, objectCursor, objectRow),
                target,
                errorTrie: null,
                selection,
                (IComplexTypeDefinition)selection.NamedType,
                depth,
                entry.ChildResultSelectionSet,
                entry.ChildSelectionSet);
            SkipValue(ref reader, objectRow);
            return completed;
        }

        AssertDepthAllowed(ref depth);

        var propertyCount = objectRow.SizeOrLength;
        var next = 0;

        for (var i = 0; i < propertyCount; i++)
        {
            var nameRow = reader.Read();
            var valueCursor = reader.Cursor;
            var valueRow = reader.Read();

            if (TryMatchEntry(program, document, nameRow, ref next, out var index))
            {
                ref readonly var childEntry = ref program[index];

                if (childEntry.RowOffset == 0)
                {
                    SkipValue(ref reader, valueRow);
                    continue;
                }

                var targetProperty = objectContext.GetPropertyAt(childEntry.RowOffset);

                if (!TryCompleteProgramValue(document, ref reader, valueCursor, valueRow, in childEntry, targetProperty, depth))
                {
                    return false;
                }
            }
            else
            {
                if (!objectContext.TryGetProperty(GetName(document, nameRow), out var targetProperty, out var selection))
                {
                    SkipValue(ref reader, valueRow);
                    continue;
                }

                var completed = TryCompleteUnprogrammedValue(document, valueCursor, valueRow, program, selection, targetProperty, depth);
                SkipValue(ref reader, valueRow);

                if (!completed)
                {
                    return false;
                }
            }
        }

        // The object's end row.
        reader.Skip(1);
        return true;
    }

    private bool TryCompleteListWithProgram(
        SourceResultDocument document,
        ref SequentialReader reader,
        DbRow arrayRow,
        in MergeProgramEntry entry,
        CompositeResultElement target,
        int depth)
    {
        AssertDepthAllowed(ref depth);

        var selection = entry.Selection!;
        var length = arrayRow.SizeOrLength;

        if (target.ValueKind is JsonValueKind.Undefined)
        {
            target.SetArrayValue(length);
        }
        else if (target.ValueKind is not JsonValueKind.Array
            || target.GetArrayLength() != length)
        {
            var error = ErrorBuilder.New()
                .SetMessage("Cannot merge shared list results with different lengths.")
                .SetPath(target.CompactPath.ToPath(target.Operation))
                .Build();
            error = _errorHandler.Handle(error);
            _store.AddError(error);
            SkipValue(ref reader, arrayRow);

            return !_propagateNullValues;
        }

        var isNonNull = selection.IsNonNullListElement;
        using var targetEnumerator = target.EnumerateArray().GetEnumerator();

        for (var i = 0; i < length; i++)
        {
            targetEnumerator.MoveNext();

            var elementCursor = reader.Cursor;
            var elementRow = reader.Read();
            var targetElement = targetEnumerator.Current;

            if (elementRow.TokenType is JsonTokenType.Null)
            {
                if (isNonNull && _propagateNullValues)
                {
                    return false;
                }

                targetElement.SetNullValue();
                continue;
            }

            bool completed;

            switch (entry.Op)
            {
                case MergeOp.ObjectList:
                    if (elementRow.TokenType is JsonTokenType.StartObject)
                    {
                        completed = TryCompleteObjectWithProgram(
                            document,
                            ref reader,
                            elementCursor,
                            elementRow,
                            in entry,
                            targetElement,
                            depth);
                    }
                    else
                    {
                        completed = TryCompleteObjectValue(
                            new SourceResultElementSnapshot(document, elementCursor, elementRow),
                            targetElement,
                            errorTrie: null,
                            selection,
                            (IComplexTypeDefinition)selection.NamedType,
                            depth,
                            entry.ChildResultSelectionSet,
                            entry.ChildSelectionSet);
                        SkipValue(ref reader, elementRow);
                    }

                    break;

                case MergeOp.LeafList:
                    targetElement.SetLeafValue(new SourceResultElementSnapshot(document, elementCursor, elementRow));
                    SkipValue(ref reader, elementRow);
                    completed = true;
                    break;

                default:
                    CompleteEnumValue(
                        new SourceResultElementSnapshot(document, elementCursor, elementRow),
                        targetElement,
                        selection);
                    SkipValue(ref reader, elementRow);
                    completed = true;
                    break;
            }

            if (!completed)
            {
                if (isNonNull)
                {
                    return false;
                }

                targetElement.SetNullValue();
            }
        }

        // The array's end row.
        reader.Skip(1);
        return true;
    }

    /// <summary>
    /// Matches a property name against the program entry expected next, and by name when the
    /// prediction fails. <paramref name="next"/> is moved behind the matched entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryMatchEntry(
        MergeProgram program,
        SourceResultDocument document,
        DbRow nameRow,
        ref int next,
        out int index)
    {
        var name = GetName(document, nameRow);

        if (next < program.Length && program.NameEquals(next, name))
        {
            index = next++;
            return true;
        }

        if (program.TryFind(name, out index))
        {
            next = index + 1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the raw name of a property name row. Names are stored with their quotes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetName(SourceResultDocument document, DbRow nameRow)
        => document.ReadRawValue(nameRow.Location, nameRow.SizeOrLength)[1..^1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SkipValue(ref SequentialReader reader, DbRow valueRow)
    {
        if (!valueRow.IsSimpleValue)
        {
            reader.Skip(valueRow.NumberOfRows - 1);
        }
    }
}
