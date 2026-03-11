using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class SelectionVisitor<TContext> : SelectionVisitor where TContext : ISelectionVisitorContext
{
    protected virtual ISelectionVisitorAction Visit(
        IOutputFieldDefinition field,
        TContext context)
    {
        var localContext = OnBeforeEnter(field, context);
        var result = Enter(field, localContext);
        localContext = OnAfterEnter(field, localContext, result);

        if (result.Kind is SelectionVisitorActionKind.Continue)
        {
            if (VisitChildren(field, context).Kind is SelectionVisitorActionKind.Break)
            {
                return Break;
            }
        }

        if (result.Kind is SelectionVisitorActionKind.Continue or SelectionVisitorActionKind.SkipAndLeave)
        {
            localContext = OnBeforeLeave(field, localContext);
            result = Leave(field, localContext);
            OnAfterLeave(field, localContext, result);
        }

        return result;
    }

    protected virtual TContext OnBeforeLeave(
        IOutputFieldDefinition field,
        TContext localContext)
        => localContext;

    protected virtual TContext OnAfterLeave(
        IOutputFieldDefinition field,
        TContext localContext,
        ISelectionVisitorAction result)
        => localContext;

    protected virtual TContext OnAfterEnter(
        IOutputFieldDefinition field,
        TContext localContext,
        ISelectionVisitorAction result)
        => localContext;

    protected virtual TContext OnBeforeEnter(
        IOutputFieldDefinition field,
        TContext context)
        => context;

    protected virtual ISelectionVisitorAction Visit(
        Selection selection,
        TContext context)
    {
        var localContext = OnBeforeEnter(selection, context);
        var result = Enter(selection, localContext);
        localContext = OnAfterEnter(selection, localContext, result);

        if (result.Kind is SelectionVisitorActionKind.Continue
            && VisitChildren(selection, context).Kind == SelectionVisitorActionKind.Break)
        {
            return Break;
        }

        if (result.Kind is SelectionVisitorActionKind.Continue or SelectionVisitorActionKind.SkipAndLeave)
        {
            localContext = OnBeforeLeave(selection, localContext);
            result = Leave(selection, localContext);
            OnAfterLeave(selection, localContext, result);
        }

        return result;
    }

    protected virtual TContext OnBeforeLeave(
        Selection selection,
        TContext localContext) =>
        localContext;

    protected virtual TContext OnAfterLeave(
        Selection selection,
        TContext localContext,
        ISelectionVisitorAction result) =>
        localContext;

    protected virtual TContext OnAfterEnter(
        Selection selection,
        TContext localContext,
        ISelectionVisitorAction result) =>
        localContext;

    protected virtual TContext OnBeforeEnter(
        Selection selection,
        TContext context) =>
        context;

    protected virtual ISelectionVisitorAction VisitChildren(IOutputFieldDefinition field, TContext context)
    {
        var type = field.Type;
        var selection = context.Selections.Peek();

        var namedType = type.NamedType();
        if (namedType.IsAbstractType())
        {
            foreach (var possibleType in
                context.ResolverContext.Schema.GetPossibleTypes(field.Type.NamedType()))
            {
                var result = VisitObjectType(field, possibleType, selection, context);

                if (result != Continue)
                {
                    return result;
                }
            }
        }
        else if (namedType is ObjectType a)
        {
            return VisitObjectType(field, a, selection, context);
        }

        return DefaultAction;
    }

    protected virtual ISelectionVisitorAction VisitObjectType(
        IOutputFieldDefinition field,
        ObjectType objectType,
        Selection selection,
        TContext context)
    {
        context.ResolvedTypes.Push(field.Type.NamedType().IsAbstractType() ? objectType : null);

        try
        {
            var selectionSet = selection.GetSelectionSet(objectType);
            var includeFlags = context.ResolverContext.IncludeFlags;

            foreach (var childSelection in selectionSet.Selections)
            {
                if (childSelection.IsSkipped(includeFlags))
                {
                    continue;
                }

                if (Visit(childSelection, context).Kind is SelectionVisitorActionKind.Break)
                {
                    return Break;
                }
            }
        }
        finally
        {
            context.ResolvedTypes.Pop();
        }

        return DefaultAction;
    }

    protected virtual ISelectionVisitorAction VisitChildren(
        Selection selection,
        TContext context)
    {
        return Visit(selection.Field, context);
    }

    protected virtual ISelectionVisitorAction Enter(
        IOutputFieldDefinition field,
        TContext context) =>
        DefaultAction;

    protected virtual ISelectionVisitorAction Leave(
        IOutputFieldDefinition field,
        TContext context) =>
        DefaultAction;

    protected virtual ISelectionVisitorAction Enter(
        Selection selection,
        TContext context)
    {
        context.Selections.Push(selection);
        return DefaultAction;
    }

    protected virtual ISelectionVisitorAction Leave(
        Selection selection,
        TContext context)
    {
        context.Selections.Pop();
        return DefaultAction;
    }
}
