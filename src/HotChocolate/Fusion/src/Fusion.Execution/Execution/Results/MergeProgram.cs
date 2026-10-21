using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// The response names of a source schema selection set in the order the source schema
/// writes them, with the nested selection sets of object fields.
/// </summary>
internal sealed class MergeProgramTemplate
{
    private MergeProgramTemplate(ImmutableArray<Entry> entries)
    {
        Entries = entries;
    }

    public ImmutableArray<Entry> Entries { get; }

    /// <summary>
    /// Creates the template for the selection set that <paramref name="path"/> reaches in
    /// <paramref name="operation"/>. Returns <c>null</c> when the path does not reach a
    /// selection set.
    /// </summary>
    public static MergeProgramTemplate? TryCreate(OperationDefinitionNode operation, SelectionPath path)
    {
        var current = operation.SelectionSet;

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];
            SelectionSetNode? next = null;

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.InlineFragment:
                    foreach (var selection in current.Selections)
                    {
                        if (selection is InlineFragmentNode fragment
                            && fragment.TypeCondition?.Name.Value == segment.Name)
                        {
                            next = fragment.SelectionSet;
                            break;
                        }
                    }

                    break;

                case SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field:
                    foreach (var selection in current.Selections)
                    {
                        if (selection is FieldNode field
                            && (field.Alias?.Value ?? field.Name.Value) == segment.Name)
                        {
                            next = field.SelectionSet;
                            break;
                        }
                    }

                    break;
            }

            if (next is null)
            {
                return null;
            }

            current = next;
        }

        return Create(current);
    }

    public static MergeProgramTemplate Create(SelectionSetNode selectionSet)
    {
        var entries = ImmutableArray.CreateBuilder<Entry>();
        Collect(selectionSet, entries);
        return new MergeProgramTemplate(entries.ToImmutable());
    }

    private static void Collect(SelectionSetNode selectionSet, ImmutableArray<Entry>.Builder entries)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var responseName = field.Alias?.Value ?? field.Name.Value;

                    if (Contains(entries, responseName))
                    {
                        continue;
                    }

                    entries.Add(
                        new Entry(
                            responseName,
                            field.SelectionSet is null ? null : Create(field.SelectionSet)));
                    break;

                case InlineFragmentNode fragment:
                    Collect(fragment.SelectionSet, entries);
                    break;
            }
        }
    }

    private static bool Contains(ImmutableArray<Entry>.Builder entries, string responseName)
    {
        foreach (var entry in entries)
        {
            if (string.Equals(entry.ResponseName, responseName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A selected field: its response name and the template of its selection set. Fields of
    /// inline fragments are listed with the direct fields, in document order.
    /// </summary>
    public sealed record Entry(string ResponseName, MergeProgramTemplate? Child);
}

/// <summary>
/// The operation codes a <see cref="MergeProgramEntry"/> completes a source value with.
/// </summary>
internal enum MergeOp : byte
{
    /// <summary>The composite selection set does not select the field.</summary>
    Skip,

    /// <summary>Any value that is not a leaf is completed by the generic completion.</summary>
    Generic,

    /// <summary>An object field whose child selection set has a program.</summary>
    Object,

    /// <summary>A list of objects whose child selection set has a program.</summary>
    ObjectList,

    /// <summary>A list of scalar values.</summary>
    LeafList,

    /// <summary>A list of enum values.</summary>
    EnumList
}

/// <summary>
/// One field of a <see cref="MergeProgram"/>: the response name to expect, the target value
/// row relative to the object's start row and how a non-leaf source value is completed.
/// </summary>
internal readonly struct MergeProgramEntry
{
    internal MergeProgramEntry(
        int nameOffset,
        int nameLength,
        int rowOffset,
        Selection? selection,
        MergeOp op,
        SelectionSet? childSelectionSet,
        MergeProgram? child,
        ResultSelectionSet? childResultSelectionSet)
    {
        NameOffset = nameOffset;
        NameLength = nameLength;
        RowOffset = rowOffset;
        Selection = selection;
        Op = op;
        ChildSelectionSet = childSelectionSet;
        Child = child;
        ChildResultSelectionSet = childResultSelectionSet;
    }

    public int NameOffset { get; }

    public int NameLength { get; }

    /// <summary>
    /// The value row of the field, counted from the object's start row. Zero when the composite
    /// selection set does not select the field.
    /// </summary>
    public int RowOffset { get; }

    public Selection? Selection { get; }

    public MergeOp Op { get; }

    public SelectionSet? ChildSelectionSet { get; }

    public MergeProgram? Child { get; }

    public ResultSelectionSet? ChildResultSelectionSet { get; }
}

/// <summary>
/// The fields of one composite selection set in source response order, resolved from a
/// <see cref="MergeProgramTemplate"/>. A program only predicts the next property; a property
/// that does not match is resolved by name.
/// </summary>
internal sealed class MergeProgram
{
    private readonly MergeProgramEntry[] _entries;
    private readonly byte[] _names;

    private MergeProgram(
        SelectionSet selectionSet,
        ResultSelectionSet? resultSelectionSet,
        bool isRoot,
        MergeProgramEntry[] entries,
        byte[] names)
    {
        SelectionSet = selectionSet;
        ResultSelectionSet = resultSelectionSet;
        IsRoot = isRoot;
        _entries = entries;
        _names = names;
    }

    public SelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the result selection set the fields of <see cref="SelectionSet"/> are completed
    /// with, or <c>null</c> below a pruned result selection set.
    /// </summary>
    public ResultSelectionSet? ResultSelectionSet { get; }

    /// <summary>
    /// Gets whether the program completes the object a result is merged into, whose child
    /// result selection sets are looked up by response name only.
    /// </summary>
    public bool IsRoot { get; }

    /// <summary>
    /// Gets the result selection set a child of <paramref name="selection"/> is completed with.
    /// </summary>
    public ResultSelectionSet? GetChildResultSelectionSet(Selection selection)
        => IsRoot
            ? ResultSelectionSet?.TryGetChild(selection.ResponseName)
            : ResultSelectionSet?.TryGetChild(selection.ResponseName, SelectionSet.Type);

    public int Length => _entries.Length;

    public ref readonly MergeProgramEntry this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _entries[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetName(in MergeProgramEntry entry)
        => _names.AsSpan(entry.NameOffset, entry.NameLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NameEquals(int index, ReadOnlySpan<byte> name)
    {
        ref readonly var entry = ref _entries[index];
        return entry.NameLength == name.Length
            && name.SequenceEqual(_names.AsSpan(entry.NameOffset, entry.NameLength));
    }

    public bool TryFind(ReadOnlySpan<byte> name, out int index)
    {
        var entries = _entries;

        for (var i = 0; i < entries.Length; i++)
        {
            if (NameEquals(i, name))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    /// <summary>
    /// Resolves <paramref name="template"/> against <paramref name="selectionSet"/>.
    /// <paramref name="resultSelectionSet"/> is the result selection set the fields of
    /// <paramref name="selectionSet"/> are completed with; <paramref name="isRoot"/> selects
    /// how its children are looked up.
    /// </summary>
    public static MergeProgram Create(
        MergeProgramTemplate template,
        SelectionSet selectionSet,
        ResultSelectionSet? resultSelectionSet,
        bool isRoot)
    {
        var templateEntries = template.Entries;
        var entries = new MergeProgramEntry[templateEntries.Length];
        var nameLength = 0;

        foreach (var templateEntry in templateEntries)
        {
            nameLength += Encoding.UTF8.GetByteCount(templateEntry.ResponseName);
        }

        var names = new byte[nameLength];
        var nameOffset = 0;
        var objectType = selectionSet.Type;

        for (var i = 0; i < templateEntries.Length; i++)
        {
            var templateEntry = templateEntries[i];
            var responseName = templateEntry.ResponseName;
            var length = Encoding.UTF8.GetBytes(responseName, names.AsSpan(nameOffset));

            if (!selectionSet.TryGetSelection(responseName, out var selection))
            {
                entries[i] = new MergeProgramEntry(
                    nameOffset,
                    length,
                    rowOffset: 0,
                    selection: null,
                    MergeOp.Skip,
                    childSelectionSet: null,
                    child: null,
                    childResultSelectionSet: null);
                nameOffset += length;
                continue;
            }

            var rowOffset = ((selection.Id - selectionSet.Id - 1) * 2) + 2;
            var childResultSelectionSet = isRoot
                ? resultSelectionSet?.TryGetChild(responseName)
                : resultSelectionSet?.TryGetChild(responseName, objectType);

            entries[i] = CreateEntry(
                nameOffset,
                length,
                rowOffset,
                selection,
                templateEntry.Child,
                childResultSelectionSet);
            nameOffset += length;
        }

        return new MergeProgram(selectionSet, resultSelectionSet, isRoot, entries, names);
    }

    private static MergeProgramEntry CreateEntry(
        int nameOffset,
        int length,
        int rowOffset,
        Selection selection,
        MergeProgramTemplate? childTemplate,
        ResultSelectionSet? childResultSelectionSet)
    {
        var op = MergeOp.Generic;
        SelectionSet? childSelectionSet = null;
        MergeProgram? child = null;

        // A child result selection set with response name mappings or opaque elements is
        // completed by the generic completion, which applies those rules.
        var childIsGeneric = childResultSelectionSet is { HasSourceResponseNameMappings: true }
            or { ProducesOpaqueElements: true };

        switch (selection.UnwrappedKind)
        {
            case TypeKind.Object when childTemplate is not null && !childIsGeneric:
                childSelectionSet = selection.GetSelectionSet((IComplexTypeDefinition)selection.NamedType);

                if (childSelectionSet is not null)
                {
                    child = Create(childTemplate, childSelectionSet, childResultSelectionSet, isRoot: false);
                    op = MergeOp.Object;
                }

                break;

            case TypeKind.List:
                switch (selection.ListElementKind)
                {
                    case TypeKind.Object when childTemplate is not null && !childIsGeneric:
                        childSelectionSet = selection.GetSelectionSet((IComplexTypeDefinition)selection.NamedType);

                        if (childSelectionSet is not null)
                        {
                            child = Create(childTemplate, childSelectionSet, childResultSelectionSet, isRoot: false);
                            op = MergeOp.ObjectList;
                        }

                        break;

                    case TypeKind.Scalar:
                        op = MergeOp.LeafList;
                        break;

                    case TypeKind.Enum:
                        op = MergeOp.EnumList;
                        break;
                }

                break;
        }

        return new MergeProgramEntry(
            nameOffset,
            length,
            rowOffset,
            selection,
            op,
            childSelectionSet,
            child,
            childResultSelectionSet);
    }
}
