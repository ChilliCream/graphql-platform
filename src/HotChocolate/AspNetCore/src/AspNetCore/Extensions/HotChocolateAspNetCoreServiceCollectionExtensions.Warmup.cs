using HotChocolate.AspNetCore.Warmup;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Internal;

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
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHostedService<RequestExecutorWarmupService>();
        builder.Services.AddSingleton(new WarmupSchemaTask(builder.Name, keepWarm, warmup));
        return builder;
    }

    public static IRequestExecutorBuilder InitializeOnStartup(
        this IRequestExecutorBuilder builder,
        RequestExecutorInitializationOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Func<IRequestExecutor, CancellationToken, Task>? warmup;

        if (options.WriteSchemaFile.Enable)
        {
            var schemaFileName =
                options.WriteSchemaFile.FileName
                    ?? System.IO.Path.Combine(Environment.CurrentDirectory, "schema.graphqls");

            if (options.Warmup is null)
            {
                warmup = async (executor, cancellationToken)
                    => await SchemaFileExporter.Export(schemaFileName, executor, cancellationToken);
            }
            else
            {
                warmup = async (executor, cancellationToken) =>
                {
                    await SchemaFileExporter.Export(schemaFileName, executor, cancellationToken);
                    await options.Warmup(executor, cancellationToken);
                };
            }
        }
        else
        {
            warmup = options.Warmup;
        }

        return InitializeOnStartup(builder, warmup, options.KeepWarm);
    }
}
