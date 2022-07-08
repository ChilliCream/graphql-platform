using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using static HotChocolate.Data.Projections.WellKnownProjectionFields;

namespace HotChocolate.Data.Projections;

public class ProjectionVisitor<TContext>
    : SelectionVisitor<TContext>
    where TContext : IProjectionVisitorContext
{
    public virtual void Visit(TContext context)
    {
        context.Selection.Push((ISelection)context.Context.Selection);
        Visit(context.Context.Selection.Field, context);
    }

    protected override TContext OnBeforeLeave(ISelection selection, TContext localContext)
    {
        if (selection is IProjectionSelection projectionSelection &&
            projectionSelection.Handler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnBeforeLeave(localContext, selection);
        }

        return localContext;
    }

    protected override TContext OnAfterLeave(
        ISelection selection,
        TContext localContext,
        ISelectionVisitorAction result)
    {
        if (selection is IProjectionSelection projectionSelection &&
            projectionSelection.Handler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnAfterLeave(localContext, selection, result);
        }

        return localContext;
    }

    protected override TContext OnAfterEnter(
        ISelection selection,
        TContext localContext,
        ISelectionVisitorAction result)
    {
        if (selection is IProjectionSelection projectionSelection &&
            projectionSelection.Handler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnAfterEnter(localContext, selection, result);
        }

        return localContext;
    }

    protected override TContext OnBeforeEnter(ISelection selection, TContext context)
    {
        if (selection is IProjectionSelection projectionSelection &&
            projectionSelection.Handler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnBeforeEnter(context, selection);
        }

        return context;
    }

    protected override ISelectionVisitorAction Enter(
        ISelection selection,
        TContext context)
    {
        base.Enter(selection, context);

        if (selection is IProjectionSelection projectionSelection &&
            projectionSelection.Handler is IProjectionFieldHandler<TContext> handler &&
            handler.TryHandleEnter(
                context,
                selection,
                out var handlerResult))
        {
            return handlerResult;
        }

        return SkipAndLeave;
    }

    protected override ISelectionVisitorAction Leave(
        ISelection selection,
        TContext context)
    {
        base.Leave(selection, context);

        if (selection is IProjectionSelection projectionSelection &&
            projectionSelection.Handler is IProjectionFieldHandler<TContext> handler &&
            handler.TryHandleLeave(
                context,
                selection,
                out var handlerResult))
        {
            return handlerResult;
        }

        return SkipAndLeave;
    }

    protected override ISelectionVisitorAction Visit(ISelection selection, TContext context)
    {
        if (selection.Field.IsNotProjected())
        {
            return Skip;
        }

        return base.Visit(selection, context);
    }

    protected override ISelectionVisitorAction Visit(IOutputField field, TContext context)
    {
        if (context.Selection.Count > 1 && field.HasProjectionMiddleware())
        {
            return Skip;
        }

        if (field.Type is IPageType and ObjectType pageType &&
            context.Selection.Peek() is { } pagingFieldSelection)
        {
            var selections = context.Context.GetSelections(pageType, pagingFieldSelection, true);

            foreach (var selection in selections)
            {
                if (selection.ResponseName is CombinedEdgeField)
                {
                    context.Selection.Push(selection);
                    return base.Visit(selection.Field, context);
                }
            }

            return Skip;
        }

        return base.Visit(field, context);
    }
}
