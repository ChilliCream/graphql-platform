using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds a warmup delegate for each newly created executor.
    /// The optional skip predicate receives the application services.
    /// </summary>
    public static IFusionRouterBuilder AddWarmupTask(
        this IFusionRouterBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task> warmupFunc,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        CoreFusionGatewayBuilderExtensions.AddWarmupTask(builder, warmupFunc, skipIf);
        return builder;
    }

    /// <summary>
    /// Adds a warmup task. The optional skip predicate receives the application services.
    /// </summary>
    public static IFusionRouterBuilder AddWarmupTask(
        this IFusionRouterBuilder builder,
        IRequestExecutorWarmupTask warmupTask,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        CoreFusionGatewayBuilderExtensions.AddWarmupTask(builder, warmupTask, skipIf);
        return builder;
    }

    /// <summary>
    /// Adds a warmup task activated using the schema services.
    /// The optional skip predicate receives the application services.
    /// </summary>
    public static IFusionRouterBuilder AddWarmupTask<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IRequestExecutorWarmupTask
    {
        CoreFusionGatewayBuilderExtensions.AddWarmupTask<T>(builder, skipIf);
        return builder;
    }

    /// <summary>
    /// Adds a warmup task created using the schema services.
    /// The optional skip predicate receives the application services.
    /// </summary>
    public static IFusionRouterBuilder AddWarmupTask<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, T> factory,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IRequestExecutorWarmupTask
    {
        CoreFusionGatewayBuilderExtensions.AddWarmupTask(builder, factory, skipIf);
        return builder;
    }
}
