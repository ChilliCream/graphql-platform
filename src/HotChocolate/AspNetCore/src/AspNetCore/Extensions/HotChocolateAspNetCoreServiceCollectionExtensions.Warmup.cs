using HotChocolate.AspNetCore.Warmup;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the current GraphQL configuration to the warmup background service.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="warmup">
    /// The warmup task that shall be executed on a new executor.
    /// </param>
    /// <param name="keepWarm">
    /// Apply warmup task after eviction and keep executor in-memory.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder InitializeOnStartup(
        this IRequestExecutorBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task>? warmup = null,
        bool keepWarm = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddHostedService<ExecutorWarmupService>();
        builder.Services.AddSingleton(new WarmupSchemaTask(builder.Name, keepWarm, warmup));
        return builder;
    }
}
