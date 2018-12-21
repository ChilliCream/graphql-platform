using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public static class ClassQueryExecutionBuilderExtensions
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
            return builder
                .AddErrorHandler()
                .AddQueryValidation()
                .AddDefaultValidationRules()
                .AddDefaultQueryCache()
                .UseDiagnostics()
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
            return builder.Use(ClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IQueryExecutionBuilder Use<TMiddleware>(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, QueryDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return builder.Use(
                ClassMiddlewareFactory.Create<TMiddleware>(factory));
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
                builder.AddQueryCache(Defaults.CacheSize);
            }
            return builder;
        }

        public static IQueryExecutionBuilder AddErrorHandler(
            this IQueryExecutionBuilder builder)
        {
            builder.Services.AddSingleton<IErrorHandler, ErrorHandler>();
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
