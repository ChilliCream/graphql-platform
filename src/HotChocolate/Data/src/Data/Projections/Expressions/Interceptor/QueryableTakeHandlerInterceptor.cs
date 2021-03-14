using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Handlers
{
    public abstract class QueryableTakeHandlerInterceptor
        : IProjectionFieldInterceptor<QueryableProjectionContext>
    {
        private readonly string _contextDataKey;
        private readonly int _take;

        public QueryableTakeHandlerInterceptor(string contextDataKey, int take)
        {
            _contextDataKey = contextDataKey;
            _take = take;
        }

        public bool CanHandle(ISelection selection) =>
            selection.Field.Member is PropertyInfo propertyInfo &&
            propertyInfo.CanWrite &&
            selection.Field.ContextData.ContainsKey(_contextDataKey);

        public void BeforeProjection(
            QueryableProjectionContext context,
            ISelection selection)
        {
            var field = selection.Field;
            if (field.ContextData.ContainsKey(_contextDataKey) &&
                selection.Field.Type.InnerType() is ListType lt &&
                lt.ElementType.InnerType() is {} elementType)
            {
                Expression instance = context.PopInstance();

                context.PushInstance(
                    Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Take),
                        new[] { elementType.ToRuntimeType() },
                        instance,
                        Expression.Constant(_take)));
            }
        }

        public void AfterProjection(
            QueryableProjectionContext context,
            ISelection selection)
        {
        }
    }
}
