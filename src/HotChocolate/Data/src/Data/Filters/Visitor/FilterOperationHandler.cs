using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class FilterOperationHandler<T, TContext>
        : FilterFieldHandler<T, TContext>
        where TContext : FilterVisitorContext<T>
    {
        public override bool TryHandleEnter(
            TContext context,
            IFilterInputType declaringType,
            IFilterField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (field is IFilterOperationField filterOperationField &&
                TryHandleOperation(
                    context, declaringType, filterOperationField, fieldType, node, out T result))
            {
                context.GetLevel().Enqueue(result);
                action = SyntaxVisitor.SkipAndLeave;
            }
            else
            {
                action = SyntaxVisitor.Skip;
            }
            return true;
        }

        public virtual bool TryHandleOperation(
            TContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out T result)
        {
            result = default;
            return false;
        }
    }
}
