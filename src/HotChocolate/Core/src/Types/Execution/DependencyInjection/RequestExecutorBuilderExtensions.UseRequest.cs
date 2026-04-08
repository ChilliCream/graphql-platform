using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a delegate that will be used to create middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="middleware">
    /// A delegate that is used to create middleware for the execution pipeline.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="before">
    /// If specified, the middleware is inserted before the middleware with the given key.
    /// </param>
    /// <param name="after">
    /// If specified, the middleware is inserted after the middleware with the given key.
    /// </param>
    /// <param name="allowMultiple">
    /// If <c>false</c> and a middleware with the same <paramref name="key"/> already exists in the
    /// pipeline, the insertion is skipped. Only applies when <paramref name="before"/> or
    /// <paramref name="after"/> is specified.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest(
        this IRequestExecutorBuilder builder,
        Func<RequestDelegate, RequestDelegate> middleware,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return Configure(
                builder,
                options => options.Pipeline.Add(
                    new RequestMiddlewareConfiguration(
                        (_, n) => middleware(n),
                        key)));
        }

        if (!allowMultiple && key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(key));
        }

        return Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration((_, n) => middleware(n), key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
                });
            });
    }

    /// <summary>
    /// Adds a delegate that will be used to create middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="middleware">
    /// A delegate that is used to create middleware for the execution pipeline.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="before">
    /// If specified, the middleware is inserted before the middleware with the given key.
    /// </param>
    /// <param name="after">
    /// If specified, the middleware is inserted after the middleware with the given key.
    /// </param>
    /// <param name="allowMultiple">
    /// If <c>false</c> and a middleware with the same <paramref name="key"/> already exists in the
    /// pipeline, the insertion is skipped. Only applies when <paramref name="before"/> or
    /// <paramref name="after"/> is specified.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest(
        this IRequestExecutorBuilder builder,
        RequestMiddleware middleware,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return Configure(
                builder,
                options => options.Pipeline.Add(new RequestMiddlewareConfiguration(middleware, key)));
        }

        if (!allowMultiple && key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(key));
        }

        return Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration(middleware, key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
                });
            });
    }

    /// <summary>
    /// Adds middleware configuration to the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="configuration">
    /// The middleware configuration to use.
    /// </param>
    /// <param name="before">
    /// If specified, the middleware is inserted before the middleware with the given key.
    /// </param>
    /// <param name="after">
    /// If specified, the middleware is inserted after the middleware with the given key.
    /// </param>
    /// <param name="allowMultiple">
    /// If <c>false</c> and a middleware with the same key already exists in the pipeline,
    /// the insertion is skipped. Only applies when <paramref name="before"/> or
    /// <paramref name="after"/> is specified.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest(
        this IRequestExecutorBuilder builder,
        RequestMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return Configure(builder, options => options.Pipeline.Add(configuration));
        }

        if (!allowMultiple && configuration.Key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(configuration));
        }

        return Configure(
            builder,
            options =>
            {
                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, configuration.Key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
                });
            });
    }

    /// <summary>
    /// Adds a type that will be used to create middleware for the execution pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware to add.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="before">
    /// If specified, the middleware is inserted before the middleware with the given key.
    /// </param>
    /// <param name="after">
    /// If specified, the middleware is inserted after the middleware with the given key.
    /// </param>
    /// <param name="allowMultiple">
    /// If <c>false</c> and a middleware with the same <paramref name="key"/> already exists in the
    /// pipeline, the insertion is skipped. Only applies when <paramref name="before"/> or
    /// <paramref name="after"/> is specified.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        TMiddleware>(
        this IRequestExecutorBuilder builder,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return Configure(
                builder,
                options => options.Pipeline.Add(
                    new RequestMiddlewareConfiguration(
                        RequestClassMiddlewareFactory.Create<TMiddleware>(),
                        key)));
        }

        if (!allowMultiple && key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(key));
        }

        return Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration(
                    RequestClassMiddlewareFactory.Create<TMiddleware>(),
                    key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
                });
            });
    }

    private static int GetIndex(IList<RequestMiddlewareConfiguration> pipeline, string key)
    {
        for (var i = 0; i < pipeline.Count; i++)
        {
            if (pipeline[i].Key == key)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Adds a middleware that will be used to cache the GraphQL operation document.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseDocumentCache(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentCache);
    }

    /// <summary>
    /// Adds a middleware that will be used to parse the GraphQL operation document.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseDocumentParser(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentParser);
    }

    /// <summary>
    /// Adds a middleware that will be used to validate the GraphQL operation document.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseDocumentValidation(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentValidation);
    }

    /// <summary>
    /// Adds a middleware that will be used to handle exceptions.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseExceptions(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.UnhandledExceptions);
    }

    /// <summary>
    /// Adds a middleware that will be used to handle timeouts.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseTimeout(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(TimeoutMiddleware.Create());
    }

    /// <summary>
    /// Adds a middleware that will be used to instrument the request.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseInstrumentation(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.Instrumentation);
    }

    /// <summary>
    /// Adds a middleware that will be used to cache the compiled
    /// operation object that is used during the request execution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseOperationCache(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationCacheMiddleware.Create());
    }

    /// <summary>
    /// Adds a middleware that will be used to execute the operation.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseOperationExecution(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationExecutionMiddleware.Create());
    }

    /// <summary>
    /// Adds a middleware that will be used to resolve the correct operation from the GraphQL operation document
    /// and that compiles this operation definition into an executable operation.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseOperationResolver(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationResolverMiddleware.Create());
    }

    /// <summary>
    /// Adds a middleware that will be used to coerce the operation variables into the correct types.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseOperationVariableCoercion(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(OperationVariableCoercionMiddleware.Create());
    }

    /// <summary>
    /// Adds a middleware that will be used to skip the actual execution of warmup requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseSkipWarmupExecution(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.SkipWarmupExecution);
    }

    /// <summary>
    /// Adds a middleware that will be used to resolve a persisted operation from the persisted operation store.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseReadPersistedOperation(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(PersistedOperationMiddleware.ReadPersistedOperation);
    }

    public static IRequestExecutorBuilder UseAutomaticPersistedOperationNotFound(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(PersistedOperationMiddleware.AutomaticPersistedOperationNotFound);
    }

    public static IRequestExecutorBuilder UseWritePersistedOperation(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(PersistedOperationMiddleware.WritePersistedOperation);
    }

    public static IRequestExecutorBuilder UsePersistedOperationNotFound(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(PersistedOperationMiddleware.PersistedOperationNotFound);
    }

    public static IRequestExecutorBuilder UseOnlyPersistedOperationAllowed(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(PersistedOperationMiddleware.OnlyPersistedOperationsAllowed);
    }

    public static IRequestExecutorBuilder UseDefaultPipeline(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return Configure(
            builder,
            options => options.Pipeline.AddDefaultPipeline());
    }

    public static IRequestExecutorBuilder UsePersistedOperationPipeline(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
        ArgumentNullException.ThrowIfNull(builder);

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

    internal static void AddDefaultPipeline(this IList<RequestMiddlewareConfiguration> pipeline)
    {
        pipeline.Add(CommonMiddleware.Instrumentation);
        pipeline.Add(CommonMiddleware.UnhandledExceptions);
        pipeline.Add(TimeoutMiddleware.Create());
        pipeline.Add(CommonMiddleware.DocumentCache);
        pipeline.Add(CommonMiddleware.DocumentParser);
        pipeline.Add(CommonMiddleware.DocumentValidation);
        pipeline.Add(OperationCacheMiddleware.Create());
        pipeline.Add(OperationResolverMiddleware.Create());
        pipeline.Add(CommonMiddleware.SkipWarmupExecution);
        pipeline.Add(OperationVariableCoercionMiddleware.Create());
        pipeline.Add(OperationExecutionMiddleware.Create());
    }
}
