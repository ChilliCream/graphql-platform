using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldHandler<T, TContext>
        : FilterFieldHandler
        where TContext : FilterVisitorContext<T>
    {
        public virtual bool TryHandleEnter(
            TContext context,
            IFilterInputType type,
            IFilterField field,
            ObjectValueNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        public virtual bool TryHandleLeave(
            TContext context,
            IFilterInputType type,
            IFilterField field,
            ObjectValueNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = null;
            return false;
        }

        public virtual bool TryHandleOperation(
            TContext context,
            IFilterInputType type,
            IFilterOperationField field,
            IValueNode value,
            [NotNullWhen(true)] out T result)
        {
            result = default;
            return false;
        }
    }
}
