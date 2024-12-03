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
        builder.UseRequest(TimeoutMiddleware.Create());

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

    public static IRequestExecutorBuilder UseSkipWarmupExecution(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(SkipWarmupExecutionMiddleware.Create());

    public static IRequestExecutorBuilder UseReadPersistedOperation(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(ReadPersistedOperationMiddleware.Create());

    public static IRequestExecutorBuilder UseAutomaticPersistedOperationNotFound(
        this IRequestExecutorBuilder builder)
        => builder.UseRequest(next => context =>
        {
            if (context.Document is not null || context.Request.Document is not null)
            {
                return next(context);
            }

            var error = ReadPersistedOperationMiddleware_PersistedOperationNotFound();
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

    public static IRequestExecutorBuilder UseWritePersistedOperation(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(WritePersistedOperationMiddleware.Create());

    public static IRequestExecutorBuilder UsePersistedOperationNotFound(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(PersistedOperationNotFoundMiddleware.Create());

    public static IRequestExecutorBuilder UseOnlyPersistedOperationAllowed(
        this IRequestExecutorBuilder builder) =>
        builder.UseRequest(OnlyPersistedOperationsAllowedMiddleware.Create());

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

    public static IRequestExecutorBuilder UsePersistedOperationPipeline(
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
            .UseReadPersistedOperation()
            .UsePersistedOperationNotFound()
            .UseOnlyPersistedOperationAllowed()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationResolver()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseOperationExecution();
    }

    public static IRequestExecutorBuilder UseAutomaticPersistedOperationPipeline(
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
            .UseReadPersistedOperation()
            .UseAutomaticPersistedOperationNotFound()
            .UseWritePersistedOperation()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationResolver()
            .UseSkipWarmupExecution()
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
        pipeline.Add(SkipWarmupExecutionMiddleware.Create());
        pipeline.Add(OperationVariableCoercionMiddleware.Create());
        pipeline.Add(OperationExecutionMiddleware.Create());
    }
}
