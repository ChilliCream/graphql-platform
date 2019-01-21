using System;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Stitching;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public static class StitchingQueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder UseStitchingPipeline(
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
                .UseInstrumentation(options.EnableTracing)
                .UseRequestTimeout()
                .UseExceptionHandling()
                .UseQueryParser()
                .UseValidation()
                .UseOperationResolver()
                .UseCoerceVariables()
                .UseMaxComplexity()
                .UseRemoteQueryExecuter(schemaName);
        }

        public static IQueryExecutionBuilder UseRemoteQueryExecuter(
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

            return builder.Use<RemoteQueryMiddleware>();
        }
    }
}
