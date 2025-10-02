using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    public static IRequestExecutorBuilder AddWarmupTask(
        this IRequestExecutorBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task> warmupFunc)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupFunc);

        return builder.AddWarmupTask(new DelegateWarmupTask(warmupFunc));
    }

    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    public static IRequestExecutorBuilder AddWarmupTask(
        this IRequestExecutorBuilder builder,
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
    public static IRequestExecutorBuilder AddWarmupTask<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
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

    // /// <summary>
    // /// Adds the current GraphQL configuration to the warmup background service.
    // /// </summary>
    // /// <param name="builder">
    // /// The <see cref="IRequestExecutorBuilder"/>.
    // /// </param>
    // /// <param name="warmup">
    // /// The warmup task that shall be executed on a new executor.
    // /// </param>
    // /// <param name="keepWarm">
    // /// Apply warmup task after eviction and keep executor in-memory.
    // /// </param>
    // /// <param name="skipIf">
    // /// Skips the warmup task if set to true.
    // /// </param>
    // /// <returns>
    // /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    // /// </returns>
    // /// <exception cref="ArgumentNullException">
    // /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
    // /// </exception>
    // public static IRequestExecutorBuilder InitializeOnStartup(
    //     this IRequestExecutorBuilder builder,
    //     Func<IRequestExecutor, CancellationToken, Task>? warmup = null,
    //     bool keepWarm = false,
    //     bool skipIf = false)
    // {
    //     ArgumentNullException.ThrowIfNull(builder);
    //
    //     if (!skipIf)
    //     {
    //         builder.Services.AddHostedService<RequestExecutorWarmupService>();
    //         builder.Services.AddSingleton(new WarmupSchemaTask(builder.Name, keepWarm, warmup));
    //     }
    //
    //     return builder;
    // }
    //
    // /// <summary>
    // /// Adds the current GraphQL configuration to the warmup background service.
    // /// </summary>
    // /// <param name="builder">
    // /// The <see cref="IRequestExecutorBuilder"/>.
    // /// </param>
    // /// <param name="options">
    // /// The <see cref="RequestExecutorInitializationOptions"/>.
    // /// </param>
    // /// <param name="skipIf">
    // /// Skips the warmup task if set to true.
    // /// </param>
    // /// <returns>
    // /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    // /// </returns>
    // /// <exception cref="ArgumentNullException">
    // /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
    // /// </exception>
    // public static IRequestExecutorBuilder InitializeOnStartup(
    //     this IRequestExecutorBuilder builder,
    //     RequestExecutorInitializationOptions options,
    //     bool skipIf = false)
    // {
    //     ArgumentNullException.ThrowIfNull(builder);
    //
    //     if (skipIf)
    //     {
    //         return builder;
    //     }
    //
    //     Func<IRequestExecutor, CancellationToken, Task>? warmup;
    //
    //     if (options.WriteSchemaFile.Enable)
    //     {
    //         var schemaFileName =
    //             options.WriteSchemaFile.FileName
    //                 ?? System.IO.Path.Combine(Environment.CurrentDirectory, "schema.graphqls");
    //
    //         if (options.Warmup is null)
    //         {
    //             warmup = async (executor, cancellationToken)
    //                 => await SchemaFileExporter.Export(schemaFileName, executor, cancellationToken);
    //         }
    //         else
    //         {
    //             warmup = async (executor, cancellationToken) =>
    //             {
    //                 await SchemaFileExporter.Export(schemaFileName, executor, cancellationToken);
    //                 await options.Warmup(executor, cancellationToken);
    //             };
    //         }
    //     }
    //     else
    //     {
    //         warmup = options.Warmup;
    //     }
    //
    //     return InitializeOnStartup(builder, warmup, options.KeepWarm);
    // }
    //
    // /// <summary>
    // /// Exports the GraphQL schema to a file on startup or when the schema changes.
    // /// </summary>
    // /// <param name="builder">
    // /// The <see cref="IRequestExecutorBuilder"/>.
    // /// </param>
    // /// <param name="schemaFileName">
    // /// The file name of the schema file.
    // /// </param>
    // /// <returns>
    // /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    // /// </returns>
    // /// <exception cref="ArgumentNullException">
    // /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
    // /// </exception>
    // public static IRequestExecutorBuilder ExportSchemaOnStartup(
    //     this IRequestExecutorBuilder builder,
    //     string? schemaFileName = null)
    // {
    //     ArgumentNullException.ThrowIfNull(builder);
    //
    //     return InitializeOnStartup(builder, new RequestExecutorInitializationOptions
    //     {
    //         KeepWarm = true,
    //         WriteSchemaFile = new SchemaFileInitializationOptions
    //         {
    //             Enable = true,
    //             FileName = schemaFileName
    //         }
    //     });
    // }
}
