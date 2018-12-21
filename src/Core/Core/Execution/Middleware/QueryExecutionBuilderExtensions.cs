using System;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public static class ClassQueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder UseDefaultPipeline(
            this IQueryExecutionBuilder builder)
        {
            return builder
                .UseDefaultPipeline(new QueryExecutionOptions());
        }

        public static IQueryExecutionBuilder UseDefaultPipeline(
            this IQueryExecutionBuilder builder,
            IQueryExecutionOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder
                .AddErrorHandler(options)
                .AddQueryValidation(options)
                .AddDefaultValidationRules()
                .AddDefaultQueryCache()
                .UseDiagnostics()
                .UseRequestTimeout(options)
                .UseExceptionHandling()
                .UseQueryParser()
                .UseValidation()
                .UseOperationResolver()
                .UseOperationExecuter();
        }

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

        public static IQueryExecutionBuilder UseOperationExecuter(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ExecuteOperationMiddleware>();
        }

        public static IQueryExecutionBuilder UseOperationResolver(
           this IQueryExecutionBuilder builder)
        {
            return builder.Use<ResolveOperationMiddleware>();
        }

        public static IQueryExecutionBuilder UseQueryParser(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ParseQueryMiddleware>();
        }

        public static IQueryExecutionBuilder UseRequestTimeout(
            this IQueryExecutionBuilder builder,
            IRequestTimeoutOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            builder.Services
                .AddSingleton(options);

            return builder.Use<RequestTimeoutMiddleware>();
        }

        public static IQueryExecutionBuilder UseValidation(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<ValidateQueryMiddleware>();
        }

        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder)
            where TMiddleware : class
        {
            return builder.Use(ClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, QueryDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return builder.Use(ClassMiddlewareFactory.Create(factory));
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IQueryParser> factory)
        {
            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton(factory);

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
            builder
                .RemoveService<Cache<DirectiveLookup>>()
                .RemoveService<Cache<DocumentNode>>()
                .RemoveService<Cache<OperationDefinitionNode>>();
            builder.Services
                .AddSingleton(new Cache<DirectiveLookup>(size))
                .AddSingleton(new Cache<DocumentNode>(size))
                .AddSingleton(new Cache<OperationDefinitionNode>(size));

            return builder;
        }

        public static IQueryExecutionBuilder AddDefaultQueryCache(
            this IQueryExecutionBuilder builder)
        {
            if (builder.Services.Any(t =>
                t.ServiceType == typeof(Cache<DirectiveLookup>)))
            {
                builder.AddQueryCache(Defaults.CacheSize);
            }

            return builder;
        }

        public static IQueryExecutionBuilder AddErrorHandler(
            this IQueryExecutionBuilder builder,
            IErrorHandlerOptionsAccessor options)
        {
            builder.Services
                .AddSingleton<IErrorHandler, ErrorHandler>()
                .AddSingleton<IErrorHandlerOptionsAccessor>(options);
            return builder;
        }

        public static IQueryExecutionBuilder AddErrorFilter(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IErrorFilter> factory)
        {
            builder.Services.AddSingleton<IErrorFilter>(factory);
            return builder;
        }

        public static IQueryExecutionBuilder AddErrorFilter<T>(
            this IQueryExecutionBuilder builder)
            where T : class, IErrorFilter
        {
            builder.Services.AddSingleton<IErrorFilter, T>();
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
    }
}
