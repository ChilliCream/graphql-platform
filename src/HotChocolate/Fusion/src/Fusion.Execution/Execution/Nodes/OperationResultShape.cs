using System.Runtime.InteropServices;
using System.Text;
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
    private const int MaxMergedRequests = 32;

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
    /// the root field of the operation for one set of variables, or <c>null</c> when the root
    /// selection set of the operation is not a single field without an alias that has a selection
    /// set.
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
        // A property of the result is compared with the root field of every merged operation, so
        // nothing is expected of a request that merges many operations.
        if (requests.Length > MaxMergedRequests)
        {
            return null;
        }

        var mergedAliasBatches = Volatile.Read(ref _mergedAliasBatches);

        foreach (var mergedAliasBatch in mergedAliasBatches)
        {
            if (mergedAliasBatch.IsFor(requests, items))
            {
                return mergedAliasBatch.Shape;
            }
        }

        // The operations that are sent together follow from the plan, so there are few. Nothing
        // is added once the limit is reached.
        if (mergedAliasBatches.Length == MaxMergedAliasBatches)
        {
            return null;
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
            data = AddSelectionSet(
                builder,
                operations.Current.SelectionSet,
                isRoot: true,
                out rootFieldName,
                out rootFieldValue);
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
    /// <paramref name="singleFieldValue"/> the node of its value. A selection set below the root
    /// that is one inline fragment selects the objects of that fragment.
    /// </summary>
    private static int AddSelectionSet(
        SourceResultShape.Builder builder,
        Utf8SelectionSetNode selectionSet,
        bool isRoot,
        out string? singleFieldName,
        out int singleFieldValue)
    {
        var names = new List<string>();
        var children = new List<int>();
        var distinctNames = new HashSet<string>(StringComparer.Ordinal);

        // An object that is selected through one inline fragment and nothing else has the fields
        // of that fragment or no fields at all.
        if (!isRoot)
        {
            selectionSet = UnwrapSingleInlineFragment(selectionSet);
        }

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
                    ? AddSelectionSet(builder, field.SelectionSet, isRoot: false, out _, out _)
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
    /// Returns the selection set of the inline fragment when <paramref name="selectionSet"/>
    /// consists of one inline fragment, otherwise <paramref name="selectionSet"/>.
    /// </summary>
    private static Utf8SelectionSetNode UnwrapSingleInlineFragment(Utf8SelectionSetNode selectionSet)
    {
        while (true)
        {
            Utf8InlineFragmentNode fragment = default;
            var selectionCount = 0;
            var isInlineFragment = false;

            foreach (var selection in selectionSet.GetSelections())
            {
                if (++selectionCount > 1)
                {
                    break;
                }

                isInlineFragment = selection.TryGetInlineFragment(out fragment);
            }

            if (selectionCount != 1 || !isInlineFragment)
            {
                return selectionSet;
            }

            selectionSet = fragment.SelectionSet;
        }
    }

    /// <summary>
    /// The expected shape of the result of a batched request that merges the operations
    /// <see cref="Shapes"/>.
    /// </summary>
    private sealed class MergedAliasBatch(OperationResultShape[] shapes, SourceResultShape? shape, bool isByItem)
    {
        /// <summary>
        /// The shape that every item of the batch expects when <see cref="IsByItem"/> is
        /// <c>true</c>, otherwise the distinct shapes of the merged requests in the order in which
        /// the items of the batch first expect them.
        /// </summary>
        public OperationResultShape[] Shapes { get; } = shapes;

        public SourceResultShape? Shape { get; } = shape;

        /// <summary>
        /// Gets whether the shape is for batches in which every request has one item, whose
        /// properties are expected by the position of their item.
        /// </summary>
        public bool IsByItem { get; } = isByItem;

        public bool IsFor(ReadOnlySpan<SourceSchemaClientRequest> requests, ReadOnlySpan<AliasBatchItem> items)
        {
            var shapes = Shapes;

            if (IsByItem)
            {
                if (items.Length != shapes.Length || !HasOneItemPerRequest(items))
                {
                    return false;
                }

                for (var i = 0; i < items.Length; i++)
                {
                    if (!ReferenceEquals(shapes[i], requests[items[i].RequestIndex].ResultShape))
                    {
                        return false;
                    }
                }

                return true;
            }

            var matched = 0;
            var lastRequestIndex = -1;
            var hasRequestWithSeveralItems = false;

            foreach (ref readonly var item in items)
            {
                // The items of a request follow each other.
                if (item.RequestIndex == lastRequestIndex)
                {
                    hasRequestWithSeveralItems = true;
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

            return hasRequestWithSeveralItems && matched == shapes.Length;
        }

        public static MergedAliasBatch Create(
            ReadOnlySpan<SourceSchemaClientRequest> requests,
            ReadOnlySpan<AliasBatchItem> items)
            => HasOneItemPerRequest(items)
                ? CreateByItem(requests, items)
                : CreateByRootField(requests, items);

        /// <summary>
        /// Creates the shape of a batch in which every request has one item. The position of an
        /// item then tells its request, so the root fields may share a name.
        /// </summary>
        private static MergedAliasBatch CreateByItem(
            ReadOnlySpan<SourceSchemaClientRequest> requests,
            ReadOnlySpan<AliasBatchItem> items)
        {
            var shapes = new OperationResultShape[items.Length];
            var names = new string[items.Length];
            var children = new int[items.Length];
            var builder = new SourceResultShape.Builder();
            var isExpected = true;

            for (var i = 0; i < items.Length; i++)
            {
                var resultShape = requests[items[i].RequestIndex].ResultShape!;
                shapes[i] = resultShape;

                if (resultShape._rootFieldName is not { } rootFieldName)
                {
                    isExpected = false;
                    continue;
                }

                names[i] = rootFieldName;
                children[i] = builder.Append(resultShape.Single) + resultShape._rootFieldValue;
            }

            if (!isExpected)
            {
                return new MergedAliasBatch(shapes, shape: null, isByItem: true);
            }

            var data = builder.AddObjectOfAliasedItems(names, children);

            return new MergedAliasBatch(
                shapes,
                builder.Build(builder.AddObject(["data"], [data], identify: false)),
                isByItem: true);
        }

        private static MergedAliasBatch CreateByRootField(
            ReadOnlySpan<SourceSchemaClientRequest> requests,
            ReadOnlySpan<AliasBatchItem> items)
        {
            var shapes = new List<OperationResultShape>();
            var lastRequestIndex = -1;

            foreach (ref readonly var item in items)
            {
                // The items of a request follow each other.
                if (item.RequestIndex == lastRequestIndex)
                {
                    continue;
                }

                lastRequestIndex = item.RequestIndex;
                var resultShape = requests[lastRequestIndex].ResultShape!;

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
                // The items of a batched request are told apart by the name of their root field,
                // so no root field may have the name of another one or read as the alias of an item.
                if (resultShape._rootFieldName is not { } rootFieldName
                    || names.Contains(rootFieldName)
                    || AliasBatchPrefixHelper.TryReadIndex(Encoding.UTF8.GetBytes(rootFieldName), out _, out _))
                {
                    return new MergedAliasBatch([.. shapes], shape: null, isByItem: false);
                }

                names.Add(rootFieldName);
                children.Add(builder.Append(resultShape.Single) + resultShape._rootFieldValue);
            }

            var data = builder.AddObjectOfAliasedFields(
                CollectionsMarshal.AsSpan(names),
                CollectionsMarshal.AsSpan(children));

            return new MergedAliasBatch(
                [.. shapes],
                builder.Build(builder.AddObject(["data"], [data], identify: false)),
                isByItem: false);
        }

        private static bool HasOneItemPerRequest(ReadOnlySpan<AliasBatchItem> items)
        {
            var lastRequestIndex = -1;

            foreach (ref readonly var item in items)
            {
                // The items of a request follow each other.
                if (item.RequestIndex == lastRequestIndex)
                {
                    return false;
                }

                lastRequestIndex = item.RequestIndex;
            }

            return true;
        }
    }
}
