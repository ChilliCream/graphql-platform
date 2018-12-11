using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        private readonly List<QueryMiddleware> _middlewareComponents =
            new List<QueryMiddleware>();

        public IServiceCollection Services { get; } = new ServiceCollection();

        public IQueryExecutionBuilder Use(QueryMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewareComponents.Add(middleware);
            return this;
        }

        public IQueryExecuter Build(ISchema schema)
        {
            QueryExecutionBuilderExtensions.AddDefaultQueryCache(this);
            IServiceProvider services = Services.BuildServiceProvider();
            QueryDelegate middleware = Compile(_middlewareComponents);
            return new QueryExecuter(schema, services, middleware);
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

        public static IQueryExecutionBuilder New() =>
            new QueryExecutionBuilder();
    }

    public static class QueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder UseDiagnostics(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<DiagnosticMiddleware>();
        }

        public static IQueryExecutionBuilder UseExceptionHandling(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ExceptionMiddleware>();
        }

        public static IQueryExecutionBuilder UseQueryParser(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ParseQueryMiddleware>();
        }

        public static IQueryExecutionBuilder UseValidation(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ValidateQueryMiddleware>();
        }

        public static IQueryExecutionBuilder UseOperationResolver(
           this IQueryExecutionBuilder builder)
        {
            return builder.Use<ResolveOperationMiddleware>();
        }

        public static IQueryExecutionBuilder UseOperationExecuter(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ExecuteOperationMiddleware>();
        }

        public static IQueryExecutionBuilder UseDefaultPipeline(
            this IQueryExecutionBuilder builder)
        {
            return builder.UseDiagnostics()
                .UseExceptionHandling()
                .UseQueryParser()
                .UseValidation()
                .UseOperationResolver()
                .UseOperationExecuter();
        }

        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder)
            where TMiddleware : class
        {
            builder.Services.AddSingleton<TMiddleware>();
            builder.Use(next => Compile(typeof(TMiddleware)));
            return builder;
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IQueryParser> factory)
        {
            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton<IQueryParser>(factory);
            return builder;
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder,
            T parser)
            where T : IQueryParser
        {
            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton<IQueryParser>(parser);
            return builder;
        }

        public static IQueryExecutionBuilder AddQueryCache(
            this IQueryExecutionBuilder builder,
            int size)
        {
            builder.RemoveService<Cache<DirectiveLookup>>()
                .RemoveService<Cache<DocumentNode>>()
                .RemoveService<Cache<OperationDefinitionNode>>();

            builder.Services
                .AddSingleton<Cache<DirectiveLookup>>(
                    new Cache<DirectiveLookup>(size))
                .AddSingleton<Cache<DocumentNode>>(
                    new Cache<DocumentNode>(size))
                .AddSingleton<Cache<OperationDefinitionNode>>(
                    new Cache<OperationDefinitionNode>(size));
            return builder;
        }

        public static IQueryExecutionBuilder AddDefaultQueryCache(
            this IQueryExecutionBuilder builder)
        {
            if (builder.Services.Any(t =>
                t.ServiceType == typeof(Cache<DirectiveLookup>)))
            {
                builder.AddQueryCache(100);
            }
            return builder;
        }

        private static IQueryExecutionBuilder RemoveService<TService>(
            this IQueryExecutionBuilder builder)
        {
            return builder.RemoveService(typeof(TService));
        }

        private static IQueryExecutionBuilder RemoveService(
            this IQueryExecutionBuilder builder,
            Type serviceType)
        {
            ServiceDescriptor serviceDescriptor = builder.Services
                .FirstOrDefault(t => t.ServiceType == serviceType);
            builder.Services.Remove(serviceDescriptor);
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
