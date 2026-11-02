namespace HotChocolate.Fusion.Language;

public class FieldSelectionMapSyntaxVisitor<TContext>
    : FieldSelectionMapSyntaxVisitor
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
            FieldSelectionMapSyntaxKind.PathListValueSelection
                => Enter((PathListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ListValueSelection
                => Enter((ListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.PathObjectValueSelection
                => Enter((PathObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectValueSelection
                => Enter((ObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectFieldSelection
                => Enter((ObjectFieldSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ChoiceValueSelection
                => Enter((ChoiceValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.Argument
                => Enter((ArgumentNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectField
                => Enter((ObjectFieldNode)node, context),
            FieldSelectionMapSyntaxKind.IntValue
                => Enter((IntValueNode)node, context),
            FieldSelectionMapSyntaxKind.FloatValue
                => Enter((FloatValueNode)node, context),
            FieldSelectionMapSyntaxKind.StringValue
                => Enter((StringValueNode)node, context),
            FieldSelectionMapSyntaxKind.BooleanValue
                => Enter((BooleanValueNode)node, context),
            FieldSelectionMapSyntaxKind.NullValue
                => Enter((NullValueNode)node, context),
            FieldSelectionMapSyntaxKind.EnumValue
                => Enter((EnumValueNode)node, context),
            FieldSelectionMapSyntaxKind.ListValue
                => Enter((ListValueNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectValue
                => Enter((ObjectValueNode)node, context),
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
        PathListValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ListValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        PathObjectValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ObjectValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ObjectFieldSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ChoiceValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ArgumentNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        IntValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        FloatValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        StringValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        BooleanValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        NullValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        EnumValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ListValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Enter(
        ObjectValueNode node,
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
            FieldSelectionMapSyntaxKind.PathListValueSelection
                => Leave((PathListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ListValueSelection
                => Leave((ListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.PathObjectValueSelection
                => Leave((PathObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectValueSelection
                => Leave((ObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectFieldSelection
                => Leave((ObjectFieldSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ChoiceValueSelection
                => Leave((ChoiceValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.Argument
                => Leave((ArgumentNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectField
                => Leave((ObjectFieldNode)node, context),
            FieldSelectionMapSyntaxKind.IntValue
                => Leave((IntValueNode)node, context),
            FieldSelectionMapSyntaxKind.FloatValue
                => Leave((FloatValueNode)node, context),
            FieldSelectionMapSyntaxKind.StringValue
                => Leave((StringValueNode)node, context),
            FieldSelectionMapSyntaxKind.BooleanValue
                => Leave((BooleanValueNode)node, context),
            FieldSelectionMapSyntaxKind.NullValue
                => Leave((NullValueNode)node, context),
            FieldSelectionMapSyntaxKind.EnumValue
                => Leave((EnumValueNode)node, context),
            FieldSelectionMapSyntaxKind.ListValue
                => Leave((ListValueNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectValue
                => Leave((ObjectValueNode)node, context),
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
        PathListValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ListValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        PathObjectValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ObjectValueSelectionNode selectionNode,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ObjectFieldSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ChoiceValueSelectionNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ArgumentNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        IntValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        FloatValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        StringValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        BooleanValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        NullValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        EnumValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ListValueNode node,
        TContext context) =>
        DefaultAction;

    protected virtual ISyntaxVisitorAction Leave(
        ObjectValueNode node,
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
            FieldSelectionMapSyntaxKind.PathListValueSelection
                => VisitChildren((PathListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ListValueSelection
                => VisitChildren((ListValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.PathObjectValueSelection
                => VisitChildren((PathObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectValueSelection
                => VisitChildren((ObjectValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectFieldSelection
                => VisitChildren((ObjectFieldSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.ChoiceValueSelection
                => VisitChildren((ChoiceValueSelectionNode)node, context),
            FieldSelectionMapSyntaxKind.Argument
                => VisitChildren((ArgumentNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectField
                => VisitChildren((ObjectFieldNode)node, context),
            FieldSelectionMapSyntaxKind.IntValue
                => DefaultAction,
            FieldSelectionMapSyntaxKind.FloatValue
                => DefaultAction,
            FieldSelectionMapSyntaxKind.StringValue
                => DefaultAction,
            FieldSelectionMapSyntaxKind.BooleanValue
                => DefaultAction,
            FieldSelectionMapSyntaxKind.NullValue
                => DefaultAction,
            FieldSelectionMapSyntaxKind.EnumValue
                => DefaultAction,
            FieldSelectionMapSyntaxKind.ListValue
                => VisitChildren((ListValueNode)node, context),
            FieldSelectionMapSyntaxKind.ObjectValue
                => VisitChildren((ObjectValueNode)node, context),
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

        foreach (var argument in node.Arguments)
        {
            if (Visit(argument, node, context).IsBreak())
            {
                return Break;
            }
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
        PathListValueSelectionNode node,
        TContext context)
    {
        if (Visit(node.Path, node, context).IsBreak())
        {
            return Break;
        }

        if (Visit(node.ListValueSelection, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ListValueSelectionNode node,
        TContext context)
    {
        if (Visit(node.ElementSelection, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        PathObjectValueSelectionNode node,
        TContext context)
    {
        if (Visit(node.Path, node, context).IsBreak())
        {
            return Break;
        }

        if (Visit(node.ObjectValueSelection, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ObjectValueSelectionNode node,
        TContext context)
    {
        foreach (var field in node.Fields)
        {
            if (Visit(field, node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ObjectFieldSelectionNode node,
        TContext context)
    {
        if (Visit(node.Name, node, context).IsBreak())
        {
            return Break;
        }

        foreach (var argument in node.Arguments)
        {
            if (Visit(argument, node, context).IsBreak())
            {
                return Break;
            }
        }

        if (node.ValueSelection is not null
            && Visit(node.ValueSelection, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ChoiceValueSelectionNode node,
        TContext context)
    {
        foreach (var branch in node.Branches)
        {
            if (Visit(branch, node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ArgumentNode node,
        TContext context)
    {
        if (Visit(node.Name, node, context).IsBreak())
        {
            return Break;
        }

        if (Visit(node.Value, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ObjectFieldNode node,
        TContext context)
    {
        if (Visit(node.Name, node, context).IsBreak())
        {
            return Break;
        }

        if (Visit(node.Value, node, context).IsBreak())
        {
            return Break;
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ListValueNode node,
        TContext context)
    {
        foreach (var item in node.Items)
        {
            if (Visit(item, node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    protected virtual ISyntaxVisitorAction VisitChildren(
        ObjectValueNode node,
        TContext context)
    {
        foreach (var field in node.Fields)
        {
            if (Visit(field, node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }
}
