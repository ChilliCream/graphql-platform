using System;
using HotChocolate.Language;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class ProjectionVisitor<TContext>
        : SelectionVisitor<TContext>
        where TContext : IProjectionVisitorContext
    {
        public virtual void Visit(TContext context)
        {
            SelectionSetNode selectionSet =
                context.Context.FieldSelection.SelectionSet ?? throw new Exception();
            context.SelectionSetNodes.Push(selectionSet);
            Visit(context.Context.Field, context);
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
                    out ISelectionVisitorAction? handlerResult))
            {
                return handlerResult;
            }

            return Break;
        }

        protected override ISelectionVisitorAction Leave(
            ISelection selection,
            TContext context)
        {
            if (selection is IProjectionSelection projectionSelection &&
                projectionSelection.Handler is IProjectionFieldHandler<TContext> handler &&
                handler.TryHandleLeave(
                    context,
                    selection,
                    out ISelectionVisitorAction? handlerResult))
            {
                base.Leave(selection, context);
                return handlerResult;
            }

            return Break;
        }
    }
}
