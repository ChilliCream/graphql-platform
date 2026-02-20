// ReSharper disable RedundantSuppressNullableWarningExpression

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionCollection : ISelectionCollection
{
    private readonly Schema _schema;
    private readonly Operation _operation;
    private readonly Selection[] _selections;
    private readonly ulong _includeFlags;

    public SelectionCollection(
        Schema schema,
        Operation operation,
        Selection[] selections,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(selections);

        _includeFlags = includeFlags;
        _schema = schema;
        _operation = operation;
        _selections = selections;
    }

    public int Count => _selections.Length;

    public Selection this[int index] => _selections[index];

    ISelection IReadOnlyList<ISelection>.this[int index] => _selections[index];

    public ISelectionCollection Select(string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (!CollectSelections(fieldName, out var buffer, out var size))
        {
            return new SelectionCollection(_schema, _operation, [], _includeFlags);
        }

        var selections = new Selection[size];
        buffer.AsSpan()[..size].CopyTo(selections);
        ArrayPool<Selection>.Shared.Return(buffer);
        return new SelectionCollection(_schema, _operation, selections, _includeFlags);
    }

    public ISelectionCollection Select(ReadOnlySpan<string> fieldNames)
    {
        if (!CollectSelections(fieldNames, out var buffer, out var size))
        {
            return new SelectionCollection(_schema, _operation, [], _includeFlags);
        }

        var selections = new Selection[size];
        buffer.AsSpan()[..size].CopyTo(selections);
        ArrayPool<Selection>.Shared.Return(buffer);
        return new SelectionCollection(_schema, _operation, selections, _includeFlags);
    }

    public ISelectionCollection Select(ITypeDefinition typeContext)
    {
        ArgumentNullException.ThrowIfNull(typeContext);

        if (!CollectSelections(typeContext, out var buffer, out var size))
        {
            return new SelectionCollection(_schema, _operation, [], _includeFlags);
        }

        var selections = new Selection[size];
        buffer.AsSpan()[..size].CopyTo(selections);
        ArrayPool<Selection>.Shared.Return(buffer);
        return new SelectionCollection(_schema, _operation, selections, _includeFlags);
    }

    public bool IsSelected(string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        ref var start = ref MemoryMarshal.GetReference(_selections.AsSpan());
        ref var end = ref Unsafe.Add(ref start, _selections.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            var namedType = start.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                return false;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    if (IsChildSelected(
                        _operation,
                        _includeFlags,
                        possibleType,
                        start,
                        fieldName))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (IsChildSelected(
                    _operation,
                    _includeFlags,
                    Unsafe.As<ITypeDefinition, ObjectType>(ref namedType),
                    start,
                    fieldName))
                {
                    return true;
                }
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        return false;

        static bool IsChildSelected(
            Operation operation,
            ulong includeFlags,
            ObjectType objectType,
            Selection parent,
            string fieldName)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;

            foreach (var child in selectionSet.Selections)
            {
                if (child.IsIncluded(operationIncludeFlags)
                    && fieldName.EqualsOrdinal(child.Field.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsSelected(string fieldName1, string fieldName2)
    {
        ArgumentNullException.ThrowIfNull(fieldName1);
        ArgumentNullException.ThrowIfNull(fieldName2);

        ref var start = ref MemoryMarshal.GetReference(_selections.AsSpan());
        ref var end = ref Unsafe.Add(ref start, _selections.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (!start.Type.IsCompositeType())
            {
                return false;
            }

            var namedType = start.Type.NamedType();

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    if (IsChildSelected(
                        _operation,
                        _includeFlags,
                        possibleType,
                        start,
                        fieldName1,
                        fieldName2))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (IsChildSelected(
                    _operation,
                    _includeFlags,
                    Unsafe.As<ITypeDefinition, ObjectType>(ref namedType),
                    start,
                    fieldName1,
                    fieldName2))
                {
                    return true;
                }
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        return false;

        static bool IsChildSelected(
            Operation operation,
            ulong includeFlags,
            ObjectType objectType,
            Selection parent,
            string fieldName1,
            string fieldName2)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selections = selectionSet.Selections;

            foreach (var selection in selections)
            {
                if (selection.IsIncluded(operationIncludeFlags)
                    && (fieldName1.EqualsOrdinal(selection.Field.Name)
                    || fieldName2.EqualsOrdinal(selection.Field.Name)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsSelected(string fieldName1, string fieldName2, string fieldName3)
    {
        ArgumentNullException.ThrowIfNull(fieldName1);
        ArgumentNullException.ThrowIfNull(fieldName2);
        ArgumentNullException.ThrowIfNull(fieldName3);

        ref var start = ref MemoryMarshal.GetReference(_selections.AsSpan());
        ref var end = ref Unsafe.Add(ref start, _selections.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (!start.Type.IsCompositeType())
            {
                return false;
            }

            var namedType = start.Type.NamedType();

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    if (IsChildSelected(
                        _operation,
                        _includeFlags,
                        possibleType,
                        start,
                        fieldName1,
                        fieldName2,
                        fieldName3))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (IsChildSelected(
                    _operation,
                    _includeFlags,
                    Unsafe.As<ITypeDefinition, ObjectType>(ref namedType),
                    start,
                    fieldName1,
                    fieldName2,
                    fieldName3))
                {
                    return true;
                }
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        return false;

        static bool IsChildSelected(
            Operation operation,
            ulong includeFlags,
            ObjectType objectType,
            Selection parent,
            string fieldName1,
            string fieldName2,
            string fieldName3)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selections = selectionSet.Selections;

            foreach (var selection in selections)
            {
                if (selection.IsIncluded(operationIncludeFlags)
                    && (fieldName1.EqualsOrdinal(selection.Field.Name)
                    || fieldName2.EqualsOrdinal(selection.Field.Name)
                    || fieldName3.EqualsOrdinal(selection.Field.Name)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsSelected(ISet<string> fieldNames)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        ref var start = ref MemoryMarshal.GetReference(_selections.AsSpan());
        ref var end = ref Unsafe.Add(ref start, _selections.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (!start.Type.IsCompositeType())
            {
                return false;
            }

            var namedType = start.Type.NamedType();

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    if (IsChildSelected(_operation, _includeFlags, possibleType, start, fieldNames))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (IsChildSelected(
                    _operation,
                    _includeFlags,
                    Unsafe.As<ITypeDefinition, ObjectType>(ref namedType),
                    start,
                    fieldNames))
                {
                    return true;
                }
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        return false;

        static bool IsChildSelected(
            Operation operation,
            ulong includeFlags,
            ObjectType objectType,
            Selection parent,
            ISet<string> fieldNames)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selections = selectionSet.Selections;

            foreach (var selection in selections)
            {
                if (selection.IsIncluded(operationIncludeFlags)
                    && fieldNames.Contains(selection.Field.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private bool CollectSelections(
        string fieldName,
        out Selection[] buffer,
        out int size)
    {
        var fieldNames = ArrayPool<string>.Shared.Rent(1);
        var fieldNamesSpan = fieldNames.AsSpan()[..1];
        fieldNamesSpan[0] = fieldName;

        var result = CollectSelections(fieldNamesSpan, out buffer, out size);
        ArrayPool<string>.Shared.Return(fieldNames);
        return result;
    }

    private bool CollectSelections(
        ReadOnlySpan<string> fieldNames,
        out Selection[] buffer,
        out int size)
    {
        buffer = ArrayPool<Selection>.Shared.Rent(4);
        size = 0;

        foreach (var selection in _selections)
        {
            var namedType = selection.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                continue;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    var selectionSet = _operation.GetSelectionSet(selection, possibleType);
                    CollectFields(fieldNames, _includeFlags, ref buffer, selectionSet, size, out var written);
                    size += written;
                }
            }
            else
            {
                var objectType = Unsafe.As<ITypeDefinition, ObjectType>(ref namedType);
                var selectionSet = _operation.GetSelectionSet(selection, objectType);
                CollectFields(fieldNames, _includeFlags, ref buffer, selectionSet, size, out var written);
                size += written;
            }
        }

        if (size == 0)
        {
            ArrayPool<Selection>.Shared.Return(buffer);
            buffer = [];
        }

        return size > 0;
    }

    private bool CollectSelections(
        ITypeDefinition typeContext,
        out Selection[] buffer,
        out int size)
    {
        buffer = ArrayPool<Selection>.Shared.Rent(_selections.Length);
        size = 0;

        ref var start = ref MemoryMarshal.GetReference(_selections.AsSpan());
        ref var end = ref Unsafe.Add(ref start, _selections.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (typeContext.IsAssignableFrom(start.Type.NamedType()))
            {
                buffer[size++] = start;
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        if (size == 0)
        {
            ArrayPool<Selection>.Shared.Return(buffer);
            buffer = [];
        }

        return size > 0;
    }

    private static void CollectFields(
        ReadOnlySpan<string> fieldNames,
        ulong includeFlags,
        ref Selection[] buffer,
        SelectionSet selectionSet,
        int index,
        out int written)
    {
        written = 0;

        var selections = selectionSet.Selections;

        EnsureCapacity(ref buffer, index, selections.Length);

        foreach (var selection in selections)
        {
            foreach (var fieldName in fieldNames)
            {
                if (selection.IsIncluded(includeFlags)
                    && selection.Field.Name.EqualsOrdinal(fieldName))
                {
                    buffer[index++] = selection;
                    written++;
                }
            }
        }
    }

    private static void EnsureCapacity(ref Selection[] buffer, int index, int requiredSpace)
    {
        var capacity = buffer.Length - index;

        if (capacity >= requiredSpace)
        {
            return;
        }

        while (capacity < requiredSpace)
        {
            capacity *= 2;
        }

        var newBuffer = ArrayPool<Selection>.Shared.Rent(capacity);
        buffer.AsSpan()[..index].CopyTo(newBuffer);
        ArrayPool<Selection>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    public IEnumerator<ISelection> GetEnumerator()
        => ((IEnumerable<ISelection>)_selections).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
