using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    public static IFusionGatewayBuilder AddWarmupTask(
        this IFusionGatewayBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task> warmupFunc)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupFunc);

        return builder.AddWarmupTask(new DelegateWarmupTask(warmupFunc));
    }

    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    public static IFusionGatewayBuilder AddWarmupTask(
        this IFusionGatewayBuilder builder,
        IRequestExecutorWarmupTask warmupTask)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupTask);

        builder.ConfigureSchemaServices((_, sc) => sc.AddSingleton(warmupTask));

        return builder;
    }

    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    public static IFusionGatewayBuilder AddWarmupTask<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IRequestExecutorWarmupTask
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices(
            static (_, sc) => sc.AddSingleton<IRequestExecutorWarmupTask, T>());

        return builder;
    }

    private sealed class DelegateWarmupTask(Func<IRequestExecutor, CancellationToken, Task> warmupFunc)
        : IRequestExecutorWarmupTask
    {
        public bool ApplyOnlyOnStartup => false;

        public Task WarmupAsync(IRequestExecutor requestExecutor, CancellationToken cancellationToken)
        {
            return warmupFunc.Invoke(requestExecutor, cancellationToken);
        }
    }
}
