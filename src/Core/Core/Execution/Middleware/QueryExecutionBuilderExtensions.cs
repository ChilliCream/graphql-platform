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
                .AddQueryCache(options)
                .AddExecutionStrategyResolver()
                .AddDefaultParser()
                .UseDiagnostics()
                .UseRequestTimeout(options)
                .UseExceptionHandling()
                .UseQueryParser()
                .UseValidation()
                .UseOperationResolver()
                .UseCoerceVariables()
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

        public static IQueryExecutionBuilder UseCoerceVariables(
            this IQueryExecutionBuilder builder)
        {
            return builder.Use<CoerceVariablesMiddleware>();
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
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.Use(ClassMiddlewareFactory.Create(factory));
        }

        public static IQueryExecutionBuilder AddExecutionStrategyResolver(
            this IQueryExecutionBuilder builder)
        {
            builder.RemoveService<IExecutionStrategyResolver>();
            builder.Services.AddSingleton<
                IExecutionStrategyResolver,
                ExecutionStrategyResolver>();
            return builder;
        }

        public static IQueryExecutionBuilder AddDefaultParser(
            this IQueryExecutionBuilder builder)
        {
            return AddParser<DefaultQueryParser>(builder);
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IQueryParser> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton(factory);

            return builder;
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder,
            T parser)
            where T : class, IQueryParser
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton<IQueryParser>(parser);

            return builder;
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder)
            where T : class, IQueryParser
        {
            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton<IQueryParser, T>();

            return builder;
        }

        public static IQueryExecutionBuilder AddQueryCache(
            this IQueryExecutionBuilder builder,
            IQueryCacheSizeOptionsAccessor options)
        {
            return AddQueryCache(builder, options.QueryCacheSize);
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

        public static IQueryExecutionBuilder AddErrorHandler(
            this IQueryExecutionBuilder builder,
            IErrorHandlerOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            builder
                .RemoveService<IErrorHandler>()
                .RemoveService<IErrorHandlerOptionsAccessor>();

            builder.Services
                .AddSingleton<IErrorHandler, ErrorHandler>()
                .AddSingleton<IErrorHandlerOptionsAccessor>(options);

            return builder;
        }

        public static IQueryExecutionBuilder AddErrorFilter(
            this IQueryExecutionBuilder builder,
            Func<IError, Exception, IError> errorFilter)
        {
            if (errorFilter == null)
            {
                throw new ArgumentNullException(nameof(errorFilter));
            }

            builder.Services.AddSingleton<IErrorFilter>(
                new FuncErrorFilterWrapper(errorFilter));
            return builder;
        }

        public static IQueryExecutionBuilder AddErrorFilter(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IErrorFilter> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

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
