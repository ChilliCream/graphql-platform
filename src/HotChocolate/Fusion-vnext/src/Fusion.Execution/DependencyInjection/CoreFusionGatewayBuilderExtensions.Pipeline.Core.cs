using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        Func<RequestDelegate, RequestDelegate> middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options => options.PipelineModifiers.Add(
                pipeline => pipeline.Add(
                    new RequestMiddlewareConfiguration((_, n) => middleware(n), key))));
    }

    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        RequestMiddleware middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options => options.PipelineModifiers.Add(
                pipeline => pipeline.Add(
                    new RequestMiddlewareConfiguration(middleware, key))));
    }

    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        RequestMiddlewareConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return Configure(
            builder,
            options => options.PipelineModifiers.Add(
                pipeline => pipeline.Add(configuration)));
    }

    public static IFusionGatewayBuilder AppendUseRequest(
        this IFusionGatewayBuilder builder,
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

    public static IFusionGatewayBuilder AppendUseRequest(
        this IFusionGatewayBuilder builder,
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

    public static IFusionGatewayBuilder InsertUseRequest(
        this IFusionGatewayBuilder builder,
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

    public static IFusionGatewayBuilder InsertUseRequest(
        this IFusionGatewayBuilder builder,
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
            options => options.PipelineModifiers.AddDefaultPipeline());
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


}
