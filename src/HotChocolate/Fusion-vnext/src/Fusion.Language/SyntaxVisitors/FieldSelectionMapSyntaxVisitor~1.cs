namespace HotChocolate.Fusion.Language;

public class FieldSelectionMapSyntaxVisitor<TContext> : FieldSelectionMapSyntaxVisitor
    , ISyntaxVisitor<TContext>
{
    public FieldSelectionMapSyntaxVisitor() : this(Skip)
    {
    }

    public FieldSelectionMapSyntaxVisitor(ISyntaxVisitorAction defaultAction)
    {
        DefaultAction = defaultAction;
    }

    /// <summary>
    /// The visitor default action.
    /// </summary>
    protected virtual ISyntaxVisitorAction DefaultAction { get; }

    public ISyntaxVisitorAction Visit(IFieldSelectionMapSyntaxNode node, TContext context)
        => Visit<IFieldSelectionMapSyntaxNode, IFieldSelectionMapSyntaxNode?>(node, null, context);

    protected ISyntaxVisitorAction Visit<TNode, TParent>(
        TNode node,
        TParent parent,
        TContext context)
        where TNode : IFieldSelectionMapSyntaxNode
        where TParent : IFieldSelectionMapSyntaxNode?
    {
        var localContext = OnBeforeEnter(node, parent, context);
        var result = Enter(node, localContext);
        localContext = OnAfterEnter(node, parent, localContext, result);

        if (result.Kind == SyntaxVisitorActionKind.Continue)
        {
            if (VisitChildren(node, context).Kind == SyntaxVisitorActionKind.Break)
            {
                return Break;
            }
        }

        if (result.Kind is SyntaxVisitorActionKind.Continue or SyntaxVisitorActionKind.SkipAndLeave)
        {
            localContext = OnBeforeLeave(node, parent, localContext);
            result = Leave(node, localContext);
            OnAfterLeave(node, parent, localContext, result);
        }

        return result;
    }

    protected virtual ISyntaxVisitorAction Enter(
        IFieldSelectionMapSyntaxNode node,
        TContext context)
    {
        return node.Kind switch
        {
            FieldSelectionMapSyntaxKind.Name
                => Enter((NameNode)node, context),
            FieldSelectionMapSyntaxKind.Path
                => Enter((PathNode)node, context),
            FieldSelectionMapSyntaxKind.PathSegment
                => Enter((PathSegmentNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedListValue
                => Enter((ListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedObjectField
                => Enter((SelectedObjectFieldNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedObjectValue
                => Enter((ObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ChoiceValueSelection
                => Enter((ChoiceValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedValueEntry
                => Enter((SelectedValueEntryNode)node, context),
            _ => throw new NotSupportedException(node.GetType().FullName)
        };
    }

    protected virtual ISyntaxVisitorAction Enter(
        NameNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        PathNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        PathSegmentNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ListValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        SelectedObjectFieldNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ObjectValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ChoiceValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        SelectedValueEntryNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        IFieldSelectionMapSyntaxNode node,
        TContext context)
    {
        return node.Kind switch
        {
            FieldSelectionMapSyntaxKind.Name
                => Leave((NameNode)node, context),
            FieldSelectionMapSyntaxKind.Path
                => Leave((PathNode)node, context),
            FieldSelectionMapSyntaxKind.PathSegment
                => Leave((PathSegmentNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedListValue
                => Leave((ListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedObjectField
                => Leave((SelectedObjectFieldNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedObjectValue
                => Leave((ObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ChoiceValueSelection
                => Leave((ChoiceValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedValueEntry
                => Leave((SelectedValueEntryNode)node, context),
            _ => throw new NotSupportedException(node.GetType().FullName)
        };
    }

    protected virtual ISyntaxVisitorAction Leave(
        NameNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        PathNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        PathSegmentNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ListValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        SelectedObjectFieldNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ObjectValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ChoiceValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        SelectedValueEntryNode node,
        TContext context) =>
        DefaultAction;

    protected virtual TContext OnBeforeEnter(
        IFieldSelectionMapSyntaxNode node,
        IFieldSelectionMapSyntaxNode? parent,
        TContext context) =>
        context;

    protected virtual TContext OnAfterEnter(
        IFieldSelectionMapSyntaxNode node,
        IFieldSelectionMapSyntaxNode? parent,
        TContext context,
        ISyntaxVisitorAction action) =>
        context;

    protected virtual TContext OnBeforeLeave(
        IFieldSelectionMapSyntaxNode node,
        IFieldSelectionMapSyntaxNode? parent,
        TContext context) =>
        context;

    protected virtual TContext OnAfterLeave(
        IFieldSelectionMapSyntaxNode node,
        IFieldSelectionMapSyntaxNode? parent,
        TContext context,
        ISyntaxVisitorAction action) =>
        context;

    protected virtual ISyntaxVisitorAction VisitChildren(
        IFieldSelectionMapSyntaxNode node,
        TContext context)
    {
        return node.Kind switch
        {
            FieldSelectionMapSyntaxKind.Name
                => DefaultAction,
            FieldSelectionMapSyntaxKind.Path
                => VisitChildren((PathNode)node, context),
            FieldSelectionMapSyntaxKind.PathSegment
                => VisitChildren((PathSegmentNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedListValue
                => VisitChildren((ListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedObjectField
                => VisitChildren((SelectedObjectFieldNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedObjectValue
                => VisitChildren((ObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ChoiceValueSelection
                => VisitChildren((ChoiceValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.SelectedValueEntry
                => VisitChildren((SelectedValueEntryNode)node, context),
            _ => throw new NotSupportedException(node.GetType().FullName)
        };
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        PathNode node,
        TContext context)
    {
        if (node.TypeName is not null && Visit(node.TypeName, node, context).IsBreak())
        {
            return Break;
        }

        if (Visit(node.PathSegment, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        PathSegmentNode node,
        TContext context)
    {
        if (Visit(node.FieldName, node, context).IsBreak())
        {
            return Break;
        }

        if (node.TypeName is not null && Visit(node.TypeName, node, context).IsBreak())
        {
            return Break;
        }

        if (node.PathSegment is not null && Visit(node.PathSegment, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        SelectedObjectFieldNode node,
        TContext context)
    {
        if (Visit(node.Name, node, context).IsBreak())
        {
            return Break;
        }

        if (node.SelectedValue is not null && Visit(node.SelectedValue, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ObjectValueSelectionNode selectionNode,
        TContext context)
    {
        foreach (var field in selectionNode.Fields)
        {
            if (Visit(field, selectionNode, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ChoiceValueSelectionNode node,
        TContext context)
    {
        if (Visit(node.Entries, node, context).IsBreak())
        {
            return Break;
        }

        if (node.SelectedValue is not null && Visit(node.SelectedValue, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        SelectedValueEntryNode node,
        TContext context)
    {
        if (node.Path is not null && Visit(node.Path, node, context).IsBreak())
        {
            return Break;
        }

        if (node.SelectedObjectValue is not null
            && Visit(node.SelectedObjectValue, node, context).IsBreak())
        {
            return Break;
        }

        if (node.SelectedListValue is not null
            && Visit(node.SelectedListValue, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }
}
