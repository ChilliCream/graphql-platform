using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="warmupFunc">
    /// The warmup delegate to execute.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the warmup task will not be registered.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="warmupFunc"/> is <c>null</c>.
    /// </exception>
    public static IFusionGatewayBuilder AddWarmupTask(
        this IFusionGatewayBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task> warmupFunc,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupFunc);

        return builder.AddWarmupTask(new DelegateRequestExecutorWarmupTask(warmupFunc), skipIf);
    }

    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="warmupTask">
    /// The warmup task to execute.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the warmup task will not be registered.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="warmupTask"/> is <c>null</c>.
    /// </exception>
    public static IFusionGatewayBuilder AddWarmupTask(
        this IFusionGatewayBuilder builder,
        IRequestExecutorWarmupTask warmupTask,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupTask);

        if (skipIf)
        {
            return builder;
        }

        return builder.ConfigureSchemaServices((_, sc) => sc.AddSingleton(warmupTask));
    }

    /// <summary>
    /// Adds a warmup task for the request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the warmup task will not be registered.
    /// </param>
    /// <typeparam name="T">
    /// The warmup task to execute.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IFusionGatewayBuilder AddWarmupTask<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        T>(
        this IFusionGatewayBuilder builder,
        bool skipIf = false)
        where T : class, IRequestExecutorWarmupTask
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (skipIf)
        {
            return builder;
        }

        builder.ConfigureSchemaServices(static (_, sc) => sc.AddSingleton<IRequestExecutorWarmupTask, T>());

        return builder;
    }
}
