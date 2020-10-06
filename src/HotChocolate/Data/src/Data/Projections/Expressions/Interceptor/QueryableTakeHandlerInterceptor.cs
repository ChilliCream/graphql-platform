using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;

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
            selection.Field.ContextData.ContainsKey(_contextDataKey);

        public void BeforeProjection(QueryableProjectionContext context, ISelection selection)
        {
        }

        public void AfterProjection(QueryableProjectionContext context, ISelection selection)
        {
            var field = selection.Field;
            if (field.ContextData.ContainsKey(_contextDataKey))
            {
                Type elementType = selection.Field.RuntimeType;

                Expression instance = context.PopInstance();
                context.PushInstance(
                    Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Take),
                        new[] { elementType },
                        instance,
                        Expression.Constant(_take)));
            }
        }
    }
}
