using System.Runtime.InteropServices;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The shapes that the JSON results of a source schema operation are expected to have.
/// </summary>
internal sealed class OperationResultShape
{
    private OperationResultShape(SourceResultShape single, SourceResultShape? aliasBatch)
    {
        Single = single;
        AliasBatch = aliasBatch;
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

        if (operation.SelectionSet.Selections is not [FieldNode] || rootFieldValue == SourceResultShape.NoNode)
        {
            return new OperationResultShape(builder.Build(singleRoot), aliasBatch: null);
        }

        var aliasBatchRoot = builder.AddObject(
            ["data"],
            [builder.AddObjectOfAnyFields(rootFieldValue)],
            identify: false);
        var single = builder.Build(singleRoot);
        return new OperationResultShape(single, single.WithRoot(aliasBatchRoot));
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
}
