using System.Diagnostics.CodeAnalysis;
using HotChocolate.AspNetCore.Warmup;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="warmupFunc">
    /// The warmup delegate to execute.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the warmup task will not be registered.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="warmupFunc"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddWarmupTask(
        this IRequestExecutorBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task> warmupFunc,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupFunc);

        return builder.AddWarmupTask(new DelegateWarmupTask(warmupFunc), skipIf);
    }

    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="warmupTask">
    /// The warmup task to execute.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the warmup task will not be registered.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="warmupTask"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddWarmupTask(
        this IRequestExecutorBuilder builder,
        IRequestExecutorWarmupTask warmupTask,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupTask);

        if (skipIf)
        {
            return builder;
        }

        builder.ConfigureSchemaServices((_, sc) => sc.AddSingleton(warmupTask));

        return builder;
    }

    /// <summary>
    /// Adds a warmup task for the request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the warmup task will not be registered.
    /// </param>
    /// <typeparam name="T">
    /// The warmup task to execute.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddWarmupTask<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder,
        bool skipIf = false)
        where T : class, IRequestExecutorWarmupTask
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (skipIf)
        {
            return builder;
        }

        builder.ConfigureSchemaServices(
            static (_, sc) => sc.AddSingleton<IRequestExecutorWarmupTask, T>());

        return builder;
    }

    /// <summary>
    /// Exports the GraphQL schema to a file on startup or when the schema changes.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="schemaFileName">
    /// The file name of the schema file.
    /// </param>
    /// <param name="skipIf">
    /// If <c>true</c>, the schema file will not be exported.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder ExportSchemaOnStartup(
        this IRequestExecutorBuilder builder,
        string? schemaFileName = null,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        schemaFileName ??= System.IO.Path.Combine(Environment.CurrentDirectory, "schema.graphqls");

        return builder.AddWarmupTask(new SchemaFileExporterWarmupTask(schemaFileName), skipIf);
    }
}
