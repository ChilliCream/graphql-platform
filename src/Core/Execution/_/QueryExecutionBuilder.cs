using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        private readonly List<QueryMiddleware> _middlewareComponents =
            new List<QueryMiddleware>();

        public IServiceCollection Services =>
            throw new System.NotImplementedException();

        public IQueryExecutionBuilder Use(QueryMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewareComponents.Add(middleware);
            return this;
        }

        public IQueryExecuter BuildQueryExecuter(ISchema schema)
        {
            return new QueryExecuter(schema, Compile(_middlewareComponents));
        }

        private QueryDelegate Compile(IEnumerable<QueryMiddleware> components)
        {
            QueryDelegate current = context => Task.CompletedTask;

            foreach (QueryMiddleware component in components.Reverse())
            {
                current = component(current);
            }

            return current;
        }
    }

    public static class QueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder)
            where TMiddleware : class
        {
            builder.Services.AddSingleton<TMiddleware>();
            builder.Use(next => Compile(typeof(TMiddleware)));
            return builder;
        }

        private static QueryDelegate Compile(Type middleware)
        {
            MethodInfo method = middleware.GetMethod("InvokeAsync")
                ?? middleware.GetMethod("Invoke");

            if (method == null)
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The provided middleware type must contain " +
                    "an invoke method.");
            }

            var context = Expression.Parameter(typeof(IQueryContext));
            var services = Expression.Parameter(typeof(IServiceProvider));
            var type = Expression.Parameter(typeof(Type));

            var middlewareInstance = Expression.Call(
                services,
                typeof(IServiceProvider).GetMethod("GetService"),
                type);

            var middlewareCall = Expression.Call(
                middlewareInstance,
                method,
                CreateParameters(method, context, services));

            ClassQueryDelegate call = Expression.Lambda<ClassQueryDelegate>(
                middlewareCall, context, services, type).Compile();

            return c => call(c, c.Services, middleware);
        }

        private static IEnumerable<Expression> CreateParameters(
            MethodInfo invokeMethod,
            ParameterExpression context,
            ParameterExpression services)
        {
            foreach (ParameterInfo parameter in invokeMethod.GetParameters())
            {
                if (parameter.ParameterType == typeof(IQueryContext))
                {
                    yield return context;
                }
                else
                {
                    yield return Expression.Call(
                        services,
                        typeof(IServiceProvider).GetMethod("GetService"),
                        Expression.Constant(parameter.ParameterType));
                }
            }
        }
    }
}
