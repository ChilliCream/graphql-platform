using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder UseDocumentCache(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentCache);
    }

    public static IFusionGatewayBuilder UseDocumentParser(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentParser);
    }

    public static IFusionGatewayBuilder UseDocumentValidation(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.DocumentValidation);
    }

    public static IFusionGatewayBuilder UseExceptions(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(CommonMiddleware.UnhandledExceptions);
    }

    public static IFusionGatewayBuilder UseTimeout(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseRequest(TimeoutMiddleware.Create());
    }

    public static IFusionGatewayBuilder UseInstrumentation(
        this IFusionGatewayBuilder builder)
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
