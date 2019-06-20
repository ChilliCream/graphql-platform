using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class BooleanOperationHandler
        : IExpressionOperationHandler
    { 

        public bool TryHandle(
            FilterOperation operation,
            IValueNode value,
            Expression instance,
            out Expression expression)
        {
            if (operation.Type == typeof(bool) && value is BooleanValueNode s)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);

                switch (operation.Kind)
                {
                    case FilterOperationKind.Equals:
                        expression = Expression.Equal(
                            property,
                            Expression.Constant(s.Value));
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = Expression.Equal(
                            Expression.Equal(
                                property,
                                Expression.Constant(s.Value)),
                            Expression.Constant(false));
                        return true;
                        
                }
            }

            expression = null;
            return false;
        }
    }
}
