using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public interface IExpressionOperationHandler
    {
        bool TryHandle(
            IQueryableFilterVisitorContext context,
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            Expression instance,
            out Expression expression);
    }
}
