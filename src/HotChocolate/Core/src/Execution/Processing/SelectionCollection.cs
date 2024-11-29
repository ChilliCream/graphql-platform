// ReSharper disable RedundantSuppressNullableWarningExpression

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionCollection(
    ISchema schema,
    IOperation operation,
    ISelection[] selections,
    long includeFlags)
    : ISelectionCollection
{
    private readonly ISchema _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    private readonly IOperation _operation = operation ?? throw new ArgumentNullException(nameof(operation));
    private readonly ISelection[] _selections = selections ?? throw new ArgumentNullException(nameof(selections));

    public int Count => _selections.Length;

    public ISelection this[int index] => _selections[index];

    public ISelectionCollection Select(string fieldName)
    {
        if (!CollectSelections(fieldName, out var buffer, out var size))
        {
            return new SelectionCollection(_schema, _operation, [], includeFlags);
        }

        var selections = new ISelection[size];
        buffer.AsSpan().Slice(0, size).CopyTo(selections);
        ArrayPool<ISelection>.Shared.Return(buffer);
        return new SelectionCollection(_schema, _operation, selections, includeFlags);
    }

    public ISelectionCollection Select(ReadOnlySpan<string> fieldNames)
    {
        if (!CollectSelections(fieldNames, out var buffer, out var size))
        {
            return new SelectionCollection(_schema, _operation, [], includeFlags);
        }

        var selections = new ISelection[size];
        buffer.AsSpan().Slice(0, size).CopyTo(selections);
        ArrayPool<ISelection>.Shared.Return(buffer);
        return new SelectionCollection(_schema, _operation, selections, includeFlags);
    }

    public ISelectionCollection Select(INamedType typeContext)
    {
        if (!CollectSelections(typeContext, out var buffer, out var size))
        {
            return new SelectionCollection(_schema, _operation, [], includeFlags);
        }

        var selections = new ISelection[size];
        buffer.AsSpan().Slice(0, size).CopyTo(selections);
        ArrayPool<ISelection>.Shared.Return(buffer);
        return new SelectionCollection(_schema, _operation, selections, includeFlags);
    }

    public bool IsSelected(string fieldName)
    {
        if (fieldName is null)
        {
            throw new ArgumentNullException(nameof(fieldName));
        }

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
                            includeFlags,
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
                        includeFlags,
                        (ObjectType)namedType,
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
            IOperation operation,
            long includeFlags,
            ObjectType objectType,
            ISelection parent,
            string fieldName)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selectionCount = selectionSet.Selections.Count;
            ref var start = ref ((SelectionSet)selectionSet).GetSelectionsReference();
            ref var end = ref Unsafe.Add(ref start, selectionCount);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (start.IsIncluded(operationIncludeFlags) &&
                    fieldName.EqualsOrdinal(start.Field.Name))
                {
                    return true;
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }

            return false;
        }
    }

    public bool IsSelected(string fieldName1, string fieldName2)
    {
        if (fieldName1 is null)
        {
            throw new ArgumentNullException(nameof(fieldName1));
        }

        if (fieldName2 is null)
        {
            throw new ArgumentNullException(nameof(fieldName2));
        }

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
                            includeFlags,
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
                        includeFlags,
                        (ObjectType)namedType,
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
            IOperation operation,
            long includeFlags,
            ObjectType objectType,
            ISelection parent,
            string fieldName1,
            string fieldName2)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selectionCount = selectionSet.Selections.Count;
            ref var start = ref ((SelectionSet)selectionSet).GetSelectionsReference();
            ref var end = ref Unsafe.Add(ref start, selectionCount);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (start.IsIncluded(operationIncludeFlags) &&
                    (fieldName1.EqualsOrdinal(start.Field.Name) ||
                        fieldName2.EqualsOrdinal(start.Field.Name)))
                {
                    return true;
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }

            return false;
        }
    }

    public bool IsSelected(string fieldName1, string fieldName2, string fieldName3)
    {
        if (fieldName1 is null)
        {
            throw new ArgumentNullException(nameof(fieldName1));
        }

        if (fieldName2 is null)
        {
            throw new ArgumentNullException(nameof(fieldName2));
        }

        if (fieldName3 is null)
        {
            throw new ArgumentNullException(nameof(fieldName3));
        }

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
                            includeFlags,
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
                        includeFlags,
                        (ObjectType)namedType,
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
            IOperation operation,
            long includeFlags,
            ObjectType objectType,
            ISelection parent,
            string fieldName1,
            string fieldName2,
            string fieldName3)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selectionCount = selectionSet.Selections.Count;
            ref var start = ref ((SelectionSet)selectionSet).GetSelectionsReference();
            ref var end = ref Unsafe.Add(ref start, selectionCount);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (start.IsIncluded(operationIncludeFlags) &&
                    (fieldName1.EqualsOrdinal(start.Field.Name) ||
                        fieldName2.EqualsOrdinal(start.Field.Name) ||
                        fieldName3.EqualsOrdinal(start.Field.Name)))
                {
                    return true;
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }

            return false;
        }
    }

    public bool IsSelected(ISet<string> fieldNames)
    {
        if (fieldNames is null)
        {
            throw new ArgumentNullException(nameof(fieldNames));
        }

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
                    if (IsChildSelected(_operation, includeFlags, possibleType, start, fieldNames))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (IsChildSelected(_operation, includeFlags, (ObjectType)namedType, start, fieldNames))
                {
                    return true;
                }
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }

        return false;

        static bool IsChildSelected(
            IOperation operation,
            long includeFlags,
            ObjectType objectType,
            ISelection parent,
            ISet<string> fieldNames)
        {
            var selectionSet = operation.GetSelectionSet(parent, objectType);
            var operationIncludeFlags = includeFlags;
            var selectionCount = selectionSet.Selections.Count;
            ref var start = ref ((SelectionSet)selectionSet).GetSelectionsReference();
            ref var end = ref Unsafe.Add(ref start, selectionCount);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (start.IsIncluded(operationIncludeFlags) &&
                    fieldNames.Contains(start.Field.Name))
                {
                    return true;
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }

            return false;
        }
    }

    private bool CollectSelections(
        string fieldName,
        out ISelection[] buffer,
        out int size)
    {
        var fieldNames = ArrayPool<string>.Shared.Rent(1);
        var fieldNamesSpan = fieldNames.AsSpan().Slice(0, 1);
        fieldNamesSpan[0] = fieldName;

        var result = CollectSelections(fieldNamesSpan, out buffer, out size);
        ArrayPool<string>.Shared.Return(fieldNames);
        return result;
    }

    private bool CollectSelections(
        ReadOnlySpan<string> fieldNames,
        out ISelection[] buffer,
        out int size)
    {
        buffer = ArrayPool<ISelection>.Shared.Rent(4);
        size = 0;

        ref var start = ref MemoryMarshal.GetReference(_selections.AsSpan());
        ref var end = ref Unsafe.Add(ref start, _selections.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            var namedType = start.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                goto NEXT;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in _schema.GetPossibleTypes(namedType))
                {
                    var selectionSet = _operation.GetSelectionSet(start, possibleType);
                    CollectFields(fieldNames, includeFlags, ref buffer, selectionSet, size, out var written);
                    size += written;
                }
            }
            else
            {
                var selectionSet = _operation.GetSelectionSet(start, (ObjectType)namedType);
                CollectFields(fieldNames, includeFlags, ref buffer, selectionSet, size, out var written);
                size += written;
            }

            NEXT:
            start = ref Unsafe.Add(ref start, 1)!;
        }

        if (size == 0)
        {
            ArrayPool<ISelection>.Shared.Return(buffer);
            buffer = [];
        }

        return size > 0;
    }

    private bool CollectSelections(
        INamedType typeContext,
        out ISelection[] buffer,
        out int size)
    {
        buffer = ArrayPool<ISelection>.Shared.Rent(_selections.Length);
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
            ArrayPool<ISelection>.Shared.Return(buffer);
            buffer = [];
        }

        return size > 0;
    }

    private static void CollectFields(
        ReadOnlySpan<string> fieldNames,
        long includeFlags,
        ref ISelection[] buffer,
        ISelectionSet selectionSet,
        int index,
        out int written)
    {
        written = 0;

        var operationIncludeFlags = includeFlags;
        var selectionCount = selectionSet.Selections.Count;

        ref var selectionRef = ref ((SelectionSet)selectionSet).GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selectionRef, selectionCount);

        EnsureCapacity(ref buffer, index, selectionCount);

        while (Unsafe.IsAddressLessThan(ref selectionRef, ref end))
        {
            foreach (var fieldName in fieldNames)
            {
                if (selectionRef.IsIncluded(operationIncludeFlags) &&
                    selectionRef.Field.Name.EqualsOrdinal(fieldName))
                {
                    buffer[index++] = selectionRef;
                    written++;
                }
            }

            selectionRef = ref Unsafe.Add(ref selectionRef, 1)!;
        }
    }

    private static void EnsureCapacity(ref ISelection[] buffer, int index, int requiredSpace)
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

        var newBuffer = ArrayPool<ISelection>.Shared.Rent(capacity);
        buffer.AsSpan().Slice(0, index).CopyTo(newBuffer);
        ArrayPool<ISelection>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    public IEnumerator<ISelection> GetEnumerator()
        => ((IEnumerable<ISelection>)_selections).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    private sealed class Any : INamedType
    {
        public TypeKind Kind => default!;

        public string Name => default!;

        public string Description => default!;

        public IReadOnlyDictionary<string, object?> ContextData => default!;

        public bool IsAssignableFrom(INamedType type) => true;

        public static readonly Any Instance = new Any();
    }
}
