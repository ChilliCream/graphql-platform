using System;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public static class QueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder UseDefaultPipeline(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .UseDefaultPipeline(new QueryExecutionOptions());
        }

        public static IQueryExecutionBuilder UseDefaultPipeline(
            this IQueryExecutionBuilder builder,
            IQueryExecutionOptionsAccessor options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder
                .AddOptions(options)
                .AddErrorHandler()
                .AddQueryValidation()
                .AddDefaultValidationRules()
                .AddQueryCache(options.QueryCacheSize)
                .AddExecutionStrategyResolver()
                .AddDefaultParser()
                .UseInstrumentation(options.EnableTracing)
                .UseRequestTimeout()
                .UseExceptionHandling()
                .UseQueryParser()
                .UseValidation()
                .UseOperationResolver()
                .UseCoerceVariables()
                .UseMaxComplexity()
                .UseOperationExecutor();
        }

        public static IQueryExecutionBuilder UseExceptionHandling(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<ExceptionMiddleware>();
        }

        public static IQueryExecutionBuilder UseInstrumentation(
            this IQueryExecutionBuilder builder,
            bool enableTracing)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services
                .AddScoped<DiagnosticListenerInitializer>();

            if (enableTracing)
            {
                builder.Services
                    .AddScoped<
                        IApolloTracingResultBuilder,
                        ApolloTracingResultBuilder>()
                    .AddScoped<
                        DiagnosticListener,
                        ApolloTracingDiagnosticListener>();
            }

            return builder.Use<InstrumentationMiddleware>();
        }

        public static IQueryExecutionBuilder UseOperationExecutor(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<ExecuteOperationMiddleware>();
        }

        public static IQueryExecutionBuilder UseOperationResolver(
           this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<ResolveOperationMiddleware>();
        }

        public static IQueryExecutionBuilder UseCoerceVariables(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<CoerceVariablesMiddleware>();
        }

        public static IQueryExecutionBuilder UseQueryParser(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<ParseQueryMiddleware>();
        }

        public static IQueryExecutionBuilder UseRequestTimeout(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<RequestTimeoutMiddleware>();
        }

        public static IQueryExecutionBuilder UseValidation(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<ValidateQueryMiddleware>();
        }

        public static IQueryExecutionBuilder UseMaxComplexity(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<MaxComplexityMiddleware>();
        }


        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder)
            where TMiddleware : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use(ClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, QueryDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.Use(ClassMiddlewareFactory.Create(factory));
        }

        public static IQueryExecutionBuilder UseField<TMiddleware>(
            this IQueryExecutionBuilder builder)
            where TMiddleware : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseField(
                FieldClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IQueryExecutionBuilder UseField<TMiddleware>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.UseField(
                FieldClassMiddlewareFactory.Create(factory));
        }

        public static IQueryExecutionBuilder Map(
            this IQueryExecutionBuilder builder,
            FieldReference fieldReference,
            FieldMiddleware middleware)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (fieldReference == null)
            {
                throw new ArgumentNullException(nameof(fieldReference));
            }

            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            return builder.UseField(
                FieldClassMiddlewareFactory.Create<MapMiddleware>(
                    (s, n) => new MapMiddleware(
                        n, fieldReference, middleware(n))));
        }


        public static IQueryExecutionBuilder Map<TMiddleware>(
            this IQueryExecutionBuilder builder,
            FieldReference fieldReference)
            where TMiddleware : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (fieldReference == null)
            {
                throw new ArgumentNullException(nameof(fieldReference));
            }

            return builder.UseField(
                FieldClassMiddlewareFactory.Create<MapMiddleware>(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory.Create<TMiddleware>();

                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }

        public static IQueryExecutionBuilder Map<TMiddleware>(
            this IQueryExecutionBuilder builder,
            FieldReference fieldReference,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (fieldReference == null)
            {
                throw new ArgumentNullException(nameof(fieldReference));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.UseField(
                FieldClassMiddlewareFactory.Create<MapMiddleware>(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory.Create(factory);

                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }

        public static IQueryExecutionBuilder AddExecutionStrategyResolver(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RemoveService<IExecutionStrategyResolver>();
            builder.Services.AddSingleton<
                IExecutionStrategyResolver,
                ExecutionStrategyResolver>();

            return builder;
        }

        public static IQueryExecutionBuilder AddDefaultParser(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddParser<DefaultQueryParser>(builder);
        }

        public static IQueryExecutionBuilder AddErrorHandler(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder
                .RemoveService<IErrorHandler>();
            builder.Services
                .AddSingleton<IErrorHandler, ErrorHandler>();

            return builder;
        }

        public static IQueryExecutionBuilder AddErrorFilter(
            this IQueryExecutionBuilder builder,
            Func<IError, Exception, IError> errorFilter)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddSingleton(factory);

            return builder;
        }

        public static IQueryExecutionBuilder AddErrorFilter<T>(
            this IQueryExecutionBuilder builder)
            where T : class, IErrorFilter
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IErrorFilter, T>();

            return builder;
        }

        public static IQueryExecutionBuilder AddOptions(
            this IQueryExecutionBuilder builder,
            IQueryExecutionOptionsAccessor options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            builder
                .RemoveService<IQueryExecutionOptionsAccessor>()
                .RemoveService<IErrorHandlerOptionsAccessor>()
                .RemoveService<IInstrumentationOptionsAccessor>()
                .RemoveService<IQueryCacheSizeOptionsAccessor>()
                .RemoveService<IRequestTimeoutOptionsAccessor>()
                .RemoveService<IValidateQueryOptionsAccessor>();
            builder.Services.AddOptions(options);

            return builder;
        }

        public static IServiceCollection AddOptions(
            this IServiceCollection services,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return services
                .AddSingleton(options)
                .AddSingleton<IErrorHandlerOptionsAccessor>(options)
                .AddSingleton<IInstrumentationOptionsAccessor>(options)
                .AddSingleton<IQueryCacheSizeOptionsAccessor>(options)
                .AddSingleton<IRequestTimeoutOptionsAccessor>(options)
                .AddSingleton<IValidateQueryOptionsAccessor>(options);
        }

        public static IQueryExecutionBuilder AddParser<T>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IQueryParser> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RemoveService<IQueryParser>();
            builder.Services.AddSingleton<IQueryParser, T>();

            return builder;
        }

        public static IQueryExecutionBuilder AddQueryCache(
            this IQueryExecutionBuilder builder,
            int cacheSize)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder
                .RemoveService<Cache<DirectiveMiddlewareCompiler>>()
                .RemoveService<Cache<DocumentNode>>()
                .RemoveService<Cache<OperationDefinitionNode>>();
            builder.Services
                .AddSingleton(new Cache<DirectiveMiddlewareCompiler>(cacheSize))
                .AddSingleton(new Cache<DocumentNode>(cacheSize))
                .AddSingleton(new Cache<OperationDefinitionNode>(cacheSize));

            return builder;
        }

        /// <summary>
        /// Adds a scoped service of the type <see cref="DiagnosticListener"/>
        /// with an implementation type specified in <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// A service type which derives from <see cref="DiagnosticListener"/>.
        /// </typeparam>
        /// <param name="builder">
        /// The <see cref="IQueryExecutionBuilder"/> instance which holds the
        /// <see cref="IServiceCollection"/> to add the service to.
        /// </param>
        /// <returns>
        /// A reference to this instance after the operation has completed.
        /// </returns>
        public static IQueryExecutionBuilder AddScopedDiagnosticListener<T>(
            this IQueryExecutionBuilder builder)
                where T : DiagnosticListener
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RemoveService<T>();
            builder.Services.AddScoped<DiagnosticListener, T>();

            return builder;
        }

        /// <summary>
        /// Adds a singleton service of the type
        /// <see cref="DiagnosticListener"/> with an implementation type
        /// specified in <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// A service type which derives from <see cref="DiagnosticListener"/>.
        /// </typeparam>
        /// <param name="builder">
        /// The <see cref="IQueryExecutionBuilder"/> instance which holds the
        /// <see cref="IServiceCollection"/> to add the service to.
        /// </param>
        /// <returns>
        /// A reference to this instance after the operation has completed.
        /// </returns>
        public static IQueryExecutionBuilder AddSingletonDiagnosticListener<T>(
            this IQueryExecutionBuilder builder)
                where T : DiagnosticListener
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RemoveService<T>();
            builder.Services.AddSingleton<DiagnosticListener, T>();

            return builder;
        }

        private static IQueryExecutionBuilder RemoveService<TService>(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.RemoveService(typeof(TService));
        }

        private static IQueryExecutionBuilder RemoveService(
            this IQueryExecutionBuilder builder,
            Type serviceType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            ServiceDescriptor serviceDescriptor = builder.Services
                .FirstOrDefault(t => t.ServiceType == serviceType);

            builder.Services.Remove(serviceDescriptor);

            return builder;
        }
    }
}
