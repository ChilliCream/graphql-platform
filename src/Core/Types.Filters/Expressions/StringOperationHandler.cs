using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class StringOperationHandler
        : IExpressionOperationHandler
    {
        private static readonly MethodInfo _startsWith =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals("StartsWith")
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        private static readonly MethodInfo _endsWith =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals("EndsWith")
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            Expression instance,
            ITypeConversion converter,
            out Expression expression)
        {
            if (operation.Type == typeof(string) && value is StringValueNode s)
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

                    case FilterOperationKind.StartsWith:
                        expression = Expression.Call(
                            property,
                            _startsWith,
                            new[] { Expression.Constant(s.Value) });
                        return true;

                    case FilterOperationKind.EndsWith:
                        expression = Expression.Call(
                            property,
                            _endsWith,
                            new[] { Expression.Constant(s.Value) });
                        return true;

                    case FilterOperationKind.NotStartsWith:
                        expression = Expression.Equal(
                            Expression.Call(
                                property,
                                _startsWith,
                                new[] { Expression.Constant(s.Value) }),
                            Expression.Constant(false));
                        return true;

                    case FilterOperationKind.NotEndsWith:
                        expression = Expression.Equal(
                            Expression.Call(
                                property,
                                _endsWith,
                                new[] { Expression.Constant(s.Value) }),
                            Expression.Constant(false));
                        return true;
                }
            }

            expression = null;
            return false;
        }
    }
}
