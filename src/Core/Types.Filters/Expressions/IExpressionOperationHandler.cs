using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public interface IExpressionOperationHandler
    {
        bool TryHandle(
            FilterOperation operation,
            IValueNode value,
            Expression instance,
            out Expression expression);
    }
}
