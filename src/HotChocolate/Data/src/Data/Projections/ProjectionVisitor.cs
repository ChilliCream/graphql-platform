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
        Visit(context, context.ResolverContext.Selection);
    }

    public virtual void Visit(TContext context, Selection selection)
    {
        context.Selections.Push(selection);
        Visit(selection.Field, context);
    }

    protected override TContext OnBeforeLeave(Selection selection, TContext localContext)
    {
        if (selection.ProjectionHandler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnBeforeLeave(localContext, selection);
        }

        return localContext;
    }

    protected override TContext OnAfterLeave(
        Selection selection,
        TContext localContext,
        ISelectionVisitorAction result)
    {
        if (selection.ProjectionHandler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnAfterLeave(localContext, selection, result);
        }

        return localContext;
    }

    protected override TContext OnAfterEnter(
        Selection selection,
        TContext localContext,
        ISelectionVisitorAction result)
    {
        if (selection.ProjectionHandler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnAfterEnter(localContext, selection, result);
        }

        return localContext;
    }

    protected override TContext OnBeforeEnter(Selection selection, TContext context)
    {
        if (selection.ProjectionHandler is IProjectionFieldHandler<TContext> handler)
        {
            return handler.OnBeforeEnter(context, selection);
        }

        return context;
    }

    protected override ISelectionVisitorAction Enter(
        Selection selection,
        TContext context)
    {
        base.Enter(selection, context);

        if (selection.ProjectionHandler is IProjectionFieldHandler<TContext> handler
            && handler.TryHandleEnter(context, selection, out var handlerResult))
        {
            return handlerResult;
        }

        return SkipAndLeave;
    }

    protected override ISelectionVisitorAction Leave(
        Selection selection,
        TContext context)
    {
        base.Leave(selection, context);

        if (selection.ProjectionHandler is IProjectionFieldHandler<TContext> handler
            && handler.TryHandleLeave(context, selection, out var handlerResult))
        {
            return handlerResult;
        }

        return SkipAndLeave;
    }

    protected override ISelectionVisitorAction Visit(Selection selection, TContext context)
    {
        if (selection.Field.IsNotProjected())
        {
            return Skip;
        }

        return base.Visit(selection, context);
    }

    protected override ISelectionVisitorAction Visit(IOutputFieldDefinition field, TContext context)
    {
        if (context.Selections.Count > 1 && field.IsNotProjected())
        {
            return Skip;
        }

        if (field.Type.NamedType() is IPageType and ObjectType pageType
            && context.Selections.Peek() is { } pagingFieldSelection)
        {
            var includeFlags = context.IncludeFlags;
            var selections = context.Operation.GetSelectionSet(pagingFieldSelection, pageType).Selections;

            for (var i = selections.Length - 1; i >= 0; i--)
            {
                var selection = selections[i];

                if (selection.IsSkipped(includeFlags))
                {
                    continue;
                }

                if (selection is { ResponseName: CombinedEdgeField })
                {
                    context.Selections.Push(selection);

                    return base.Visit(selection.Field, context);
                }
            }

            return Skip;
        }

        return base.Visit(field, context);
    }
}
