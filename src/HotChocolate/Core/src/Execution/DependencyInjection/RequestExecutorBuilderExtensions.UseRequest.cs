using System;
using System.Collections.Generic;
using System.Net;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using static HotChocolate.Execution.ErrorHelper;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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
            options => options.Pipeline.Add((_, next) => middleware(next)));
    }

    /// <summary>
    /// Adds a type that will be used to create a middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
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
        builder.UseRequest(DocumentCacheMiddleware.Create());

    public static IRequestExecutorBuilder UseDocumentParser(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(DocumentParserMiddleware.Create());

    public static IRequestExecutorBuilder UseDocumentValidation(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(DocumentValidationMiddleware.Create());

    public static IRequestExecutorBuilder UseExceptions(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(ExceptionMiddleware.Create());

    public static IRequestExecutorBuilder UseTimeout(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(DocumentParserMiddleware.Create());

    public static IRequestExecutorBuilder UseInstrumentation(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(InstrumentationMiddleware.Create());

    public static IRequestExecutorBuilder UseOperationCache(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(OperationCacheMiddleware.Create());

    public static IRequestExecutorBuilder UseOperationExecution(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(OperationExecutionMiddleware.Create());

    public static IRequestExecutorBuilder UseOperationResolver(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(OperationResolverMiddleware.Create());

    public static IRequestExecutorBuilder UseOperationVariableCoercion(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(OperationVariableCoercionMiddleware.Create());

    public static IRequestExecutorBuilder UseReadPersistedQuery(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(ReadPersistedQueryMiddleware.Create());

    public static IRequestExecutorBuilder UseAutomaticPersistedQueryNotFound(
        this IRequestExecutorBuilder builder)
        => builder.UseRequest(next => context =>
        {
            if (context.Document is not null || context.Request.Document is not null)
            {
                return next(context);
            }
            
            var error = ReadPersistedQueryMiddleware_PersistedQueryNotFound();
            var result = OperationResultBuilder.CreateError(
                error,
                new Dictionary<string, object?>
                {
                    { WellKnownContextData.HttpStatusCode, HttpStatusCode.BadRequest },
                });

            context.DiagnosticEvents.RequestError(context, new GraphQLException(error));
            context.Result = result;
            return default;

        });

    public static IRequestExecutorBuilder UseWritePersistedQuery(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(WritePersistedQueryMiddleware.Create());

    public static IRequestExecutorBuilder UsePersistedQueryNotFound(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(PersistedQueryNotFoundMiddleware.Create());

    public static IRequestExecutorBuilder UseOnlyPersistedQueriesAllowed(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(OnlyPersistedQueriesAllowedMiddleware.Create());

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
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseReadPersistedQuery()
            .UsePersistedQueryNotFound()
            .UseOnlyPersistedQueriesAllowed()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseOperationExecution();
    }

    public static IRequestExecutorBuilder UseAutomaticPersistedQueryPipeline(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseReadPersistedQuery()
            .UseAutomaticPersistedQueryNotFound()
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
        pipeline.Add(InstrumentationMiddleware.Create());
        pipeline.Add(ExceptionMiddleware.Create());
        pipeline.Add(TimeoutMiddleware.Create());
        pipeline.Add(DocumentCacheMiddleware.Create());
        pipeline.Add(DocumentParserMiddleware.Create());
        pipeline.Add(DocumentValidationMiddleware.Create());
        pipeline.Add(OperationCacheMiddleware.Create());
        pipeline.Add(OperationResolverMiddleware.Create());
        pipeline.Add(OperationVariableCoercionMiddleware.Create());
        pipeline.Add(OperationExecutionMiddleware.Create());
    }
}
