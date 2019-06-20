using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public interface IExpressionOperationHandler
    {
        bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            Expression instance,
            ITypeConversion converter,
            out Expression expression);
    }
}
