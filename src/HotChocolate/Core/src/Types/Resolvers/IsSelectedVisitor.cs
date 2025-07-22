using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Resolvers;

/// <summary>
/// Represents a visitor that checks what fields are selected.
/// </summary>
public sealed class IsSelectedVisitor : SyntaxWalker<IsSelectedContext>
{
    protected override ISyntaxVisitorAction Enter(FieldNode node, IsSelectedContext context)
    {
        var selections = context.Selections.Peek();
        var responseName = node.Alias?.Value ?? node.Name.Value;

        if (!selections.IsSelected(responseName))
        {
            context.AllSelected = false;
            return Break;
        }

        if (node.SelectionSet is not null)
        {
            context.Selections.Push(selections.Select(responseName));
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, IsSelectedContext context)
    {
        if (node.SelectionSet is not null)
        {
            context.Selections.Pop();
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, IsSelectedContext context)
    {
        if (node.TypeCondition is not null)
        {
            var typeContext = context.Schema.Types[node.TypeCondition.Name.Value];
            var selections = context.Selections.Peek();
            context.Selections.Push(selections.Select(typeContext));
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, IsSelectedContext context)
    {
        if (node.TypeCondition is not null)
        {
            context.Selections.Pop();
        }

        return base.Leave(node, context);
    }

    public static IsSelectedVisitor Instance { get; } = new();
}
