using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.DotNetTypeInfoFactory;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            if (context.FieldSelection.Field.Directives.Contains(
                    SingleOrDefaultDirective.DIRECTIVE_NAME) &&
                context.FieldSelection.Field.Member is PropertyInfo propertyInfo)
            {
                Type elementType = GetInnerListType(propertyInfo.PropertyType);
                return Expression.Call(
                    typeof(Enumerable),
                    "Take",
                    new[] { elementType },
                    expression,
                    Expression.Constant(2));
            }

            return expression;
        }
    }
}
