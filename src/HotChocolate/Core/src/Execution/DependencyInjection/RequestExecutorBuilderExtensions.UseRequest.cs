using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Pipeline;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to create a middleware for the execution pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="middleware">A delegate that is used to create a middleware for the execution pipeline.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder UseRequest(
            this IRequestExecutorBuilder builder,
            RequestCoreMiddleware middleware)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            return Configure(
                builder,
                options => options.Pipeline.Add(middleware));
        }

        /// <summary>
        /// Adds a delegate that will be used to create a middleware for the execution pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="middleware">A delegate that is used to create a middleware for the execution pipeline.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder UseRequest(
            this IRequestExecutorBuilder builder,
            RequestMiddleware middleware)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            return Configure(
                builder,
                options => options.Pipeline.Add((context, next) => middleware(next)));
        }

        /// <summary>
        /// Adds a type that will be used to create a middleware for the execution pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="middleware">A type that is used to create a middleware for the execution pipeline.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder UseRequest<TMiddleware>(
            this IRequestExecutorBuilder builder)
            where TMiddleware : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return Configure(
                builder,
                options => options.Pipeline.Add(
                    RequestClassMiddlewareFactory.Create<TMiddleware>()));
        }

        public static IRequestExecutorBuilder UseDocumentCache(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<DocumentCacheMiddleware>();

        public static IRequestExecutorBuilder UseDocumentParser(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<DocumentParserMiddleware>();

        public static IRequestExecutorBuilder UseDocumentValidation(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<DocumentValidationMiddleware>();

        public static IRequestExecutorBuilder UseExceptions(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<ExceptionMiddleware>();

        public static IRequestExecutorBuilder UseInstrumentations(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<InstrumentationMiddleware>();

        public static IRequestExecutorBuilder UseOperationCache(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<OperationCacheMiddleware>();

        public static IRequestExecutorBuilder UseOperationExecution(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<OperationExecutionMiddleware>();

        public static IRequestExecutorBuilder UseOperationResolver(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<OperationResolverMiddleware>();

        public static IRequestExecutorBuilder UseOperationVariableCoercion(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<OperationVariableCoercionMiddleware>();

        public static IRequestExecutorBuilder UseReadPersistedQuery(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<ReadPersistedQueryMiddleware>();

        public static IRequestExecutorBuilder UseWritePersistedQuery(
            this IRequestExecutorBuilder builder) =>
            builder.UseRequest<WritePersistedQueryMiddleware>();

        public static IRequestExecutorBuilder UseDefaultPipeline(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return Configure(
                builder,
                options => options.Pipeline.AddDefaultPipeline());
        }

        public static IRequestExecutorBuilder UsePersistedQueryPipeline(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .UseInstrumentations()
                .UseExceptions()
                .UseDocumentCache()
                .UseReadPersistedQuery()
                .UseDocumentParser()
                .UseDocumentValidation()
                .UseOperationCache()
                .UseOperationResolver()
                .UseOperationVariableCoercion()
                .UseOperationExecution();
        }

        public static IRequestExecutorBuilder UseActivePersistedQueryPipeline(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .UseInstrumentations()
                .UseExceptions()
                .UseDocumentCache()
                .UseReadPersistedQuery()
                .UseWritePersistedQuery()
                .UseDocumentParser()
                .UseDocumentValidation()
                .UseOperationCache()
                .UseOperationResolver()
                .UseOperationVariableCoercion()
                .UseOperationExecution();
        }

        internal static void AddDefaultPipeline(this IList<RequestCoreMiddleware> pipeline)
        {
            pipeline.Add(RequestClassMiddlewareFactory.Create<InstrumentationMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<ExceptionMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<DocumentCacheMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<DocumentParserMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<DocumentValidationMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<OperationCacheMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<OperationResolverMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<OperationVariableCoercionMiddleware>());
            pipeline.Add(RequestClassMiddlewareFactory.Create<OperationExecutionMiddleware>());
        }
    }
}