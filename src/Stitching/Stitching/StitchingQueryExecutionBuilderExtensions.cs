using System;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public static class StitchingQueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder UseQueryDelegationPipeline(
            this IQueryExecutionBuilder builder,
            string schemaName)
        {
            return UseQueryDelegationPipeline(
                builder,
                new QueryExecutionOptions(),
                schemaName);
        }

        public static IQueryExecutionBuilder UseQueryDelegationPipeline(
            this IQueryExecutionBuilder builder,
            IQueryExecutionOptionsAccessor options,
            string schemaName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            return builder
                .AddOptions(options)
                .AddErrorHandler()
                .AddQueryValidation()
                .AddDefaultValidationRules()
                .AddQueryCache(options.QueryCacheSize)
                .AddExecutionStrategyResolver()
                .AddDefaultParser()
                .UseInstrumentation(options.TracingPreference)
                .UseRequestTimeout()
                .UseExceptionHandling()
                .UseQueryParser()
                .UseValidation()
                .UseOperationResolver()
                .UseMaxComplexity()
                .UseRemoteQueryExecutor(schemaName);
        }

        public static IQueryExecutionBuilder UseStitchingPipeline(
            this IQueryExecutionBuilder builder)
        {
            return UseStitchingPipeline(
                builder,
                new QueryExecutionOptions());
        }

        public static IQueryExecutionBuilder UseStitchingPipeline(
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
                .UsePropagateVariables()
                .UseDefaultPipeline(options);
        }

        public static IQueryExecutionBuilder UseRemoteQueryExecutor(
            this IQueryExecutionBuilder builder,
            string schemaName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            return builder.Use((services, next) =>
                new RemoteQueryMiddleware(next, schemaName));
        }

        public static IQueryExecutionBuilder UsePropagateVariables(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<CopyVariablesToResolverContextMiddleware>();
        }
    }
}
