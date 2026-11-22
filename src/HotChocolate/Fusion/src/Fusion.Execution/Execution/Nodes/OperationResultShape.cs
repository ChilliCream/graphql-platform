using System.Runtime.InteropServices;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The shapes that the JSON results of a source schema operation are expected to have.
/// </summary>
internal sealed class OperationResultShape
{
    private const int MaxMergedAliasBatches = 8;

    private readonly string? _rootFieldName;
    private readonly int _rootFieldValue;
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private MergedAliasBatch[] _mergedAliasBatches = [];

    private OperationResultShape(
        SourceResultShape single,
        SourceResultShape? aliasBatch,
        string? rootFieldName,
        int rootFieldValue)
    {
        Single = single;
        AliasBatch = aliasBatch;
        _rootFieldName = rootFieldName;
        _rootFieldValue = rootFieldValue;
    }

    /// <summary>
    /// Gets the expected shape of the result of the operation when it is sent on its own.
    /// </summary>
    public SourceResultShape Single { get; }

    /// <summary>
    /// Gets the expected shape of a result in which every property of <c>data</c> is the value of
    /// the root field of the operation for one set of variables, or <c>null</c> when the operation
    /// does not have exactly one root field.
    /// </summary>
    public SourceResultShape? AliasBatch { get; }

    /// <summary>
    /// Gets the expected shape of the result of a batched request that sends the root fields of
    /// the operations of <paramref name="items"/>, or <c>null</c> when nothing is expected of it.
    /// The request of the first item must expect this shape.
    /// </summary>
    public SourceResultShape? GetMergedAliasBatch(
        ReadOnlySpan<SourceSchemaClientRequest> requests,
        ReadOnlySpan<AliasBatchItem> items)
    {
        var mergedAliasBatches = Volatile.Read(ref _mergedAliasBatches);

        foreach (var mergedAliasBatch in mergedAliasBatches)
        {
            if (mergedAliasBatch.IsFor(requests, items))
            {
                return mergedAliasBatch.Shape;
            }
        }

        lock (_sync)
        {
            foreach (var mergedAliasBatch in _mergedAliasBatches)
            {
                if (mergedAliasBatch.IsFor(requests, items))
                {
                    return mergedAliasBatch.Shape;
                }
            }

            // The operations that are sent together follow from the plan, so there are few.
            if (_mergedAliasBatches.Length == MaxMergedAliasBatches)
            {
                return null;
            }

            var added = MergedAliasBatch.Create(requests, items);
            Volatile.Write(ref _mergedAliasBatches, [.. _mergedAliasBatches, added]);
            return added.Shape;
        }
    }

    /// <summary>
    /// Creates the expected result shapes of the first operation of <paramref name="document"/>.
    /// </summary>
    public static OperationResultShape Create(Utf8OperationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new SourceResultShape.Builder();
        var operations = document.GetOperations().GetEnumerator();
        var data = SourceResultShape.NoNode;
        string? rootFieldName = null;
        var rootFieldValue = SourceResultShape.NoNode;

        if (operations.MoveNext())
        {
            data = AddSelectionSet(builder, operations.Current.SelectionSet, out rootFieldName, out rootFieldValue);
        }

        var singleRoot = builder.AddObject(["data"], [data], identify: false);

        if (rootFieldName is null)
        {
            return new OperationResultShape(
                builder.Build(singleRoot),
                aliasBatch: null,
                rootFieldName: null,
                SourceResultShape.NoNode);
        }

        var aliasBatchRoot = builder.AddObject(
            ["data"],
            [builder.AddObjectOfAnyFields(rootFieldValue)],
            identify: false);
        var single = builder.Build(singleRoot);
        return new OperationResultShape(single, single.WithRoot(aliasBatchRoot), rootFieldName, rootFieldValue);
    }

    /// <summary>
    /// Adds the objects that <paramref name="selectionSet"/> selects to <paramref name="builder"/>
    /// and returns their node. When the selection set is a single field without an alias whose
    /// value has a shape, <paramref name="singleFieldName"/> is the name of that field and
    /// <paramref name="singleFieldValue"/> the node of its value.
    /// </summary>
    private static int AddSelectionSet(
        SourceResultShape.Builder builder,
        Utf8SelectionSetNode selectionSet,
        out string? singleFieldName,
        out int singleFieldValue)
    {
        var names = new List<string>();
        var children = new List<int>();
        var distinctNames = new HashSet<string>(StringComparer.Ordinal);

        // A source schema returns the fields of an object in the order of its selection set. The
        // fields that an object has are only known in advance when every selection is a field.
        var identify = true;
        var selectionCount = 0;
        var hasAlias = false;

        foreach (var selection in selectionSet.GetSelections())
        {
            selectionCount++;

            if (!selection.TryGetField(out var field))
            {
                identify = false;
                continue;
            }

            hasAlias = field.HasAlias;
            var name = hasAlias ? field.Alias! : field.Name;

            if (!SourceResultShape.Builder.IsPlainName(name) || !distinctNames.Add(name))
            {
                identify = false;
                continue;
            }

            names.Add(name);
            children.Add(
                field.HasSelectionSet
                    ? AddSelectionSet(builder, field.SelectionSet, out _, out _)
                    : SourceResultShape.NoNode);
        }

        var isSingleField = selectionCount == 1
            && names.Count == 1
            && !hasAlias
            && children[0] != SourceResultShape.NoNode;

        singleFieldName = isSingleField ? names[0] : null;
        singleFieldValue = isSingleField ? children[0] : SourceResultShape.NoNode;

        return builder.AddObject(
            CollectionsMarshal.AsSpan(names),
            CollectionsMarshal.AsSpan(children),
            identify && names.Count > 0);
    }

    /// <summary>
    /// The expected shape of the result of a batched request that merges the operations
    /// <see cref="Shapes"/>.
    /// </summary>
    private sealed class MergedAliasBatch(OperationResultShape[] shapes, SourceResultShape? shape)
    {
        /// <summary>
        /// The distinct shapes of the merged requests in the order in which the items of the
        /// batch first expect them.
        /// </summary>
        public OperationResultShape[] Shapes { get; } = shapes;

        public SourceResultShape? Shape { get; } = shape;

        public bool IsFor(ReadOnlySpan<SourceSchemaClientRequest> requests, ReadOnlySpan<AliasBatchItem> items)
        {
            var shapes = Shapes;
            var matched = 0;
            var lastRequestIndex = -1;

            foreach (ref readonly var item in items)
            {
                // The items of a request follow each other.
                if (item.RequestIndex == lastRequestIndex)
                {
                    continue;
                }

                lastRequestIndex = item.RequestIndex;
                var resultShape = requests[lastRequestIndex].ResultShape;

                if (matched < shapes.Length && ReferenceEquals(shapes[matched], resultShape))
                {
                    matched++;
                    continue;
                }

                if (Array.IndexOf(shapes, resultShape, 0, matched) < 0)
                {
                    return false;
                }
            }

            return matched == shapes.Length;
        }

        public static MergedAliasBatch Create(
            ReadOnlySpan<SourceSchemaClientRequest> requests,
            ReadOnlySpan<AliasBatchItem> items)
        {
            var shapes = new List<OperationResultShape>();

            foreach (ref readonly var item in items)
            {
                var resultShape = requests[item.RequestIndex].ResultShape!;

                if (!shapes.Contains(resultShape))
                {
                    shapes.Add(resultShape);
                }
            }

            var builder = new SourceResultShape.Builder();
            var names = new List<string>();
            var children = new List<int>();

            foreach (var resultShape in shapes)
            {
                // The items of a batched request are told apart by the name of their root field.
                if (resultShape._rootFieldName is not { } rootFieldName || names.Contains(rootFieldName))
                {
                    return new MergedAliasBatch([.. shapes], shape: null);
                }

                names.Add(rootFieldName);
                children.Add(builder.Append(resultShape.Single) + resultShape._rootFieldValue);
            }

            var data = builder.AddObjectOfAliasedFields(
                CollectionsMarshal.AsSpan(names),
                CollectionsMarshal.AsSpan(children));

            return new MergedAliasBatch(
                [.. shapes],
                builder.Build(builder.AddObject(["data"], [data], identify: false)));
        }
    }
}
