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
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest(
        this IRequestExecutorBuilder builder,
        Func<RequestDelegate, RequestDelegate> middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options => options.Pipeline.Add(
                new RequestMiddlewareConfiguration(
                    (_, n) => middleware(n),
                    key)));
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
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest(
        this IRequestExecutorBuilder builder,
        RequestMiddleware middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options => options.Pipeline.Add(new RequestMiddlewareConfiguration(middleware, key)));
    }

    /// <summary>
    /// Adds a delegate that will be used to create middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="configuration">
    /// The middleware configuration to use.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest(
        this IRequestExecutorBuilder builder,
        RequestMiddlewareConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return Configure(builder, options => options.Pipeline.Add(configuration));
    }

    /// <summary>
    /// Adds a type that will be used to create middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder UseRequest<TMiddleware>(
        this IRequestExecutorBuilder builder,
        string? key = null)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        return Configure(
            builder,
            options => options.Pipeline.Add(
                new RequestMiddlewareConfiguration(
                    RequestClassMiddlewareFactory.Create<TMiddleware>(),
                    key)));
    }

    /// <summary>
    /// Appends middleware to the execution pipeline <paramref name="after"/> the middleware with the specified key.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="after">
    /// The key of the middleware after which the new middleware will be appended.
    /// </param>
    /// <param name="middleware">
    /// The middleware to append.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="allowMultiple">
    /// If set to <c>true</c>, multiple instances of the same middleware can be appended.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AppendUseRequest(
        this IRequestExecutorBuilder builder,
        string after,
        RequestMiddleware middleware,
        string? key = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(after);
        ArgumentNullException.ThrowIfNull(middleware);

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

                    var index = GetIndex(pipeline, after);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{after}` was not found.");
                    }

                    pipeline.Insert(index + 1, configuration);
                });
            });
    }

    /// <summary>
    /// Appends middleware to the execution pipeline <paramref name="after"/> the middleware with the specified key.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="after">
    /// The key of the middleware after which the new middleware will be appended.
    /// </param>
    /// <param name="configuration">
    /// The middleware configuration to append.
    /// </param>
    /// <param name="allowMultiple">
    /// If set to <c>true</c>, multiple instances of the same middleware can be appended.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AppendUseRequest(
        this IRequestExecutorBuilder builder,
        string after,
        RequestMiddlewareConfiguration configuration,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(after);
        ArgumentNullException.ThrowIfNull(configuration);

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

                    var index = GetIndex(pipeline, after);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"The middleware with the key `{after}` was not found.");
                    }

                    pipeline.Insert(index + 1, configuration);
                });
            });
    }

    /// <summary>
    /// Appends middleware to the execution pipeline <paramref name="after"/> the middleware with the specified key.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware to append.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="after">
    /// The key of the middleware after which the new middleware will be appended.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="allowMultiple">
    /// If set to <c>true</c>, multiple instances of the same middleware can be appended.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AppendUseRequest<TMiddleware>(
        this IRequestExecutorBuilder builder,
        string after,
        string? key = null,
        bool allowMultiple = true)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(after);

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

                    var index = GetIndex(pipeline, after);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"The middleware with the key `{after}` was not found.");
                    }

                    pipeline.Insert(index + 1, configuration);
                });
            });
    }

    /// <summary>
    /// Inserts middleware to the execution pipeline <paramref name="before"/> the middleware with the specified key.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="before">
    /// The key of the middleware before which the new middleware will be inserted.
    /// </param>
    /// <param name="middleware">
    /// The middleware to insert.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="allowMultiple">
    /// If set to <c>true</c>, multiple instances of the same middleware can be inserted.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder InsertUseRequest(
        this IRequestExecutorBuilder builder,
        string before,
        RequestMiddleware middleware,
        string? key = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(before);
        ArgumentNullException.ThrowIfNull(middleware);

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

                    var index = GetIndex(pipeline, before);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{before}` was not found.");
                    }

                    pipeline.Insert(index, configuration);
                });
            });
    }

    /// <summary>
    /// Inserts middleware to the execution pipeline <paramref name="before"/> the middleware with the specified key.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="before">
    /// The key of the middleware before which the new middleware will be inserted.
    /// </param>
    /// <param name="configuration">
    /// The middleware configuration to insert.
    /// </param>
    /// <param name="allowMultiple">
    /// If set to <c>true</c>, multiple instances of the same middleware can be inserted.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder InsertUseRequest(
        this IRequestExecutorBuilder builder,
        string before,
        RequestMiddlewareConfiguration configuration,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(before);
        ArgumentNullException.ThrowIfNull(configuration);

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

                    var index = GetIndex(pipeline, before);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"The middleware with the key `{before}` was not found.");
                    }

                    pipeline.Insert(index, configuration);
                });
            });
    }

    /// <summary>
    /// Inserts middleware to the execution pipeline <paramref name="before"/> the middleware with the specified key.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware to insert.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </param>
    /// <param name="before">
    /// The key of the middleware before which the new middleware will be inserted.
    /// </param>
    /// <param name="key">
    /// A unique identifier for the middleware.
    /// </param>
    /// <param name="allowMultiple">
    /// If set to <c>true</c>, multiple instances of the same middleware can be inserted.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder InsertUseRequest<TMiddleware>(
        this IRequestExecutorBuilder builder,
        string before,
        string? key = null,
        bool allowMultiple = true)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(before);

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

                    var index = GetIndex(pipeline, before);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"The middleware with the key `{before}` was not found.");
                    }

                    pipeline.Insert(index, configuration);
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
