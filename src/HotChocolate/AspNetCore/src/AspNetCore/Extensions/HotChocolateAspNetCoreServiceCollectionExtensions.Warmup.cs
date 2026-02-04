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
    /// A function that is called to determine if the warmup service should be registered or not.
    /// If <c>true</c> is returned, the warmup task will not be registered.
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
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IRequestExecutorBuilder AddWarmupTask(
        this IRequestExecutorBuilder builder,
        Func<IRequestExecutor, CancellationToken, Task> warmupFunc,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupFunc);

        return builder.AddWarmupTask(new DelegateRequestExecutorWarmupTask(warmupFunc), skipIf);
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
    /// A function that is called to determine if the warmup service should be registered or not.
    /// If <c>true</c> is returned, the warmup task will not be registered.
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
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IRequestExecutorBuilder AddWarmupTask(
        this IRequestExecutorBuilder builder,
        IRequestExecutorWarmupTask warmupTask,
        Func<IServiceProvider, bool>? skipIf = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(warmupTask);

        return builder.ConfigureSchemaServices((applicationServices, sc) =>
        {
            var shouldSkip = skipIf?.Invoke(applicationServices) ?? false;

            if (!shouldSkip)
            {
                sc.AddSingleton(warmupTask);
            }
        });
    }

    /// <summary>
    /// Adds a warmup task for the request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="skipIf">
    /// A function that is called to determine if the warmup service should be registered or not.
    /// If <c>true</c> is returned, the warmup task will not be registered.
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
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="RequestExecutorBuilderExtensions.AddApplicationService"/>.
    /// <br />
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IRequestExecutorBuilder AddWarmupTask<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IRequestExecutorWarmupTask
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices((applicationServices, sc) =>
        {
            var shouldSkip = skipIf?.Invoke(applicationServices) ?? false;

            if (!shouldSkip)
            {
                sc.AddSingleton<IRequestExecutorWarmupTask, T>();
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds a warmup task that will be executed on each newly created request executor.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The factory to create the warmup task.
    /// </param>
    /// <param name="skipIf">
    /// A function that is called to determine if the warmup service should be registered or not.
    /// If <c>true</c> is returned, the warmup task will not be registered.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="RequestExecutorBuilderExtensions.AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// <br />
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="skipIf"/>
    /// is for the application services.
    /// </remarks>
    public static IRequestExecutorBuilder AddWarmupTask<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory,
        Func<IServiceProvider, bool>? skipIf = null)
        where T : class, IRequestExecutorWarmupTask
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices(
            (applicationServices, sc) =>
            {
                var shouldSkip = skipIf?.Invoke(applicationServices) ?? false;

                if (!shouldSkip)
                {
                    sc.AddSingleton<IRequestExecutorWarmupTask, T>(factory);
                }
            });

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

        return builder.AddWarmupTask(new SchemaFileExporterWarmupTask(schemaFileName), _ => skipIf);
    }
}
