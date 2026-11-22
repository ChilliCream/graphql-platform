using System.Runtime.InteropServices;
using HotChocolate.Fusion.Execution.Clients;
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
    /// Gets the expected shape of the result of a batched request that sends the root field of
    /// the operation of every request of <paramref name="requests"/> once for every set of
    /// variables. The first request must expect this shape. Returns <c>null</c> when nothing is
    /// expected of the result. Every request must have a <see cref="SourceSchemaClientRequest.ResultShape"/>.
    /// </summary>
    public SourceResultShape? GetMergedAliasBatch(ReadOnlySpan<SourceSchemaClientRequest> requests)
    {
        var mergedAliasBatches = Volatile.Read(ref _mergedAliasBatches);

        foreach (var mergedAliasBatch in mergedAliasBatches)
        {
            if (mergedAliasBatch.IsFor(requests))
            {
                return mergedAliasBatch.Shape;
            }
        }

        lock (_sync)
        {
            foreach (var mergedAliasBatch in _mergedAliasBatches)
            {
                if (mergedAliasBatch.IsFor(requests))
                {
                    return mergedAliasBatch.Shape;
                }
            }

            // The operations that are sent together follow from the plan, so there are few.
            if (_mergedAliasBatches.Length == MaxMergedAliasBatches)
            {
                return null;
            }

            var added = MergedAliasBatch.Create(requests);
            Volatile.Write(ref _mergedAliasBatches, [.. _mergedAliasBatches, added]);
            return added.Shape;
        }
    }

    /// <summary>
    /// Creates the expected result shapes of the first operation of the source schema document
    /// <paramref name="operationSourceText"/>.
    /// </summary>
    public static OperationResultShape Create(ReadOnlySpan<byte> operationSourceText)
    {
        var document = Utf8GraphQLParser.Parse(operationSourceText, ParserOptions.Trusted);

        foreach (var definition in document.Definitions)
        {
            if (definition is OperationDefinitionNode operation)
            {
                return Create(operation);
            }
        }

        throw new ArgumentException(
            "The source schema document has no operation.",
            nameof(operationSourceText));
    }

    /// <summary>
    /// Creates the expected result shapes of <paramref name="operation"/>.
    /// </summary>
    public static OperationResultShape Create(OperationDefinitionNode operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var builder = new SourceResultShape.Builder();
        var data = AddSelectionSet(builder, operation.SelectionSet, out var rootFieldValue);
        var singleRoot = builder.AddObject(["data"], [data], identify: false);

        if (operation.SelectionSet.Selections is not [FieldNode rootField]
            || rootFieldValue == SourceResultShape.NoNode)
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
        return new OperationResultShape(
            single,
            single.WithRoot(aliasBatchRoot),
            rootField.Name.Value,
            rootFieldValue);
    }

    /// <summary>
    /// Adds the objects that <paramref name="selectionSet"/> selects to <paramref name="builder"/>
    /// and returns their node. <paramref name="firstFieldValue"/> is the node of the value of the
    /// first field of the selection set.
    /// </summary>
    private static int AddSelectionSet(
        SourceResultShape.Builder builder,
        SelectionSetNode selectionSet,
        out int firstFieldValue)
    {
        var names = new List<string>();
        var children = new List<int>();

        // A source schema returns the fields of an object in the order of its selection set. The
        // fields that an object has are only known in advance when every selection is a field.
        var identify = true;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode field)
            {
                identify = false;
                continue;
            }

            var name = field.Alias?.Value ?? field.Name.Value;

            if (!SourceResultShape.Builder.IsPlainName(name) || names.Contains(name))
            {
                identify = false;
                continue;
            }

            names.Add(name);
            children.Add(
                field.SelectionSet is null
                    ? SourceResultShape.NoNode
                    : AddSelectionSet(builder, field.SelectionSet, out _));
        }

        firstFieldValue = children.Count > 0 ? children[0] : SourceResultShape.NoNode;

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
        /// The distinct shapes of the merged requests in the order in which the requests first
        /// expect them.
        /// </summary>
        public OperationResultShape[] Shapes { get; } = shapes;

        public SourceResultShape? Shape { get; } = shape;

        public bool IsFor(ReadOnlySpan<SourceSchemaClientRequest> requests)
        {
            var shapes = Shapes;
            var matched = 0;

            foreach (ref readonly var request in requests)
            {
                var resultShape = request.ResultShape;

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

        public static MergedAliasBatch Create(ReadOnlySpan<SourceSchemaClientRequest> requests)
        {
            var shapes = new List<OperationResultShape>();
            var isExpected = true;

            foreach (ref readonly var request in requests)
            {
                if (!shapes.Contains(request.ResultShape!))
                {
                    shapes.Add(request.ResultShape!);
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
                    isExpected = false;
                    break;
                }

                names.Add(rootFieldName);
                children.Add(builder.Append(resultShape.Single) + resultShape._rootFieldValue);
            }

            if (!isExpected)
            {
                return new MergedAliasBatch([.. shapes], shape: null);
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
