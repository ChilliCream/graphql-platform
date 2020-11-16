using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    public class SortVisitor<TContext, T>
        : SortVisitorBase<TContext, T>
        where TContext : SortVisitorContext<T>
    {
        protected override ISyntaxVisitorAction OnFieldEnter(
            TContext context,
            ISortField field,
            ObjectFieldNode node)
        {
            if (field.Handler is ISortFieldHandler<TContext, T> handler &&
                handler.TryHandleEnter(
                    context,
                    field,
                    node,
                    out ISyntaxVisitorAction? action))
            {
                return action;
            }

            return SyntaxVisitor.SkipAndLeave;
        }

        protected override ISyntaxVisitorAction OnFieldLeave(
            TContext context,
            ISortField field,
            ObjectFieldNode node)
        {
            if (field.Handler is ISortFieldHandler<TContext, T> handler &&
                handler.TryHandleLeave(
                    context,
                    field,
                    node,
                    out ISyntaxVisitorAction? action))
            {
                return action;
            }

            return SyntaxVisitor.Skip;
        }

        protected override ISyntaxVisitorAction OnOperationEnter(
            TContext context,
            ISortField field,
            ISortEnumValue? sortValue,
            EnumValueNode valueNode)
        {
            if (sortValue?.Handler is ISortOperationHandler<TContext, T> handler &&
                handler.TryHandleEnter(
                    context,
                    field,
                    sortValue,
                    valueNode,
                    out ISyntaxVisitorAction? action))
            {
                return action;
            }

            return SyntaxVisitor.Skip;
        }
    }
}
