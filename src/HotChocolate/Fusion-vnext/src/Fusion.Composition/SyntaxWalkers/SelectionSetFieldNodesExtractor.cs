using System.Collections.Immutable;
using HotChocolate.Fusion.Info;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.SyntaxWalkers;

/// <summary>
/// This class is used to extract field nodes from a selection set.
/// </summary>
internal sealed class SelectionSetFieldNodesExtractor
    : SyntaxWalker<SelectionSetFieldNodeExtractorContext>
{
    public ImmutableArray<FieldNodeInfo> ExtractFieldNodes(SelectionSetNode selectionSet)
    {
        var context = new SelectionSetFieldNodeExtractorContext();

        Visit(selectionSet, context);

        return [.. context.FieldNodes];
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        SelectionSetFieldNodeExtractorContext context)
    {
        context.FieldNamePath.Add(node.Name.Value);
        context.FieldNodes.Add(new FieldNodeInfo(node, [.. context.FieldNamePath]));

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        SelectionSetFieldNodeExtractorContext context)
    {
        context.FieldNamePath.Pop();

        return Continue;
    }
}

internal sealed class SelectionSetFieldNodeExtractorContext
{
    public List<string> FieldNamePath { get; } = [];

    public List<FieldNodeInfo> FieldNodes { get; } = [];
}
