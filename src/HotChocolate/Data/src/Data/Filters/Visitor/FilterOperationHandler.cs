using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterOperationHandler<TContext, T>
        : FilterFieldHandler<TContext, T>
        where TContext : FilterVisitorContext<T>
    {
        public override bool TryHandleEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field is IFilterOperationField filterOperationField &&
                TryHandleOperation(context, filterOperationField,  node, out T result))
            {
                context.GetLevel().Enqueue(result);
                action = SyntaxVisitor.SkipAndLeave;
            }
            else
            {
                action = SyntaxVisitor.Break;
            }

            return true;
        }

        public virtual bool TryHandleOperation(
            TContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out T result)
        {
            result = default!;
            return false;
        }
    }
}
