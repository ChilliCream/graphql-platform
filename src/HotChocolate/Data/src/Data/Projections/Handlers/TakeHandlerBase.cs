using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Handlers
{
    public class TakeHandlerBase : IListHandler
    {
        private readonly string _contextDataKey;
        private readonly int _take;

        public TakeHandlerBase(string contextDataKey, int take)
        {
            _contextDataKey = contextDataKey;
            _take = take;
        }

        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            /*
            IObjectField field = context.FieldSelection.Field;
            if (field.ContextData.ContainsKey(_contextDataKey) &&
                field.Member is PropertyInfo propertyInfo)
            {
                Type elementType =  ExtendedType.Tools.GetElementType(propertyInfo.PropertyType)!;

                return Expression.Call(
                    typeof(Enumerable),
                    "Take",
                    new[] { elementType },
                    expression,
                    Expression.Constant(_take));
            }

            return expression;
            */
            throw new Exception();
        }
    }
}
