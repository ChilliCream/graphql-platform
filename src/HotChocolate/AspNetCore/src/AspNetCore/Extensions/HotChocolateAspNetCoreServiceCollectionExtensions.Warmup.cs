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
    /// <param name="skipIf">
    /// Skips the warmup task if set to true.
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
        bool keepWarm = false,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!skipIf)
        {
            builder.Services.AddHostedService<RequestExecutorWarmupService>();
            builder.Services.AddSingleton(new WarmupSchemaTask(builder.Name, keepWarm, warmup));
        }

        return builder;
    }

    /// <summary>
    /// Adds the current GraphQL configuration to the warmup background service.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="options">
    /// The <see cref="RequestExecutorInitializationOptions"/>.
    /// </param>
    /// <param name="skipIf">
    /// Skips the warmup task if set to true.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder InitializeOnStartup(
        this IRequestExecutorBuilder builder,
        RequestExecutorInitializationOptions options,
        bool skipIf = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (skipIf)
        {
            return builder;
        }

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

    /// <summary>
    /// Exports the GraphQL schema to a file on startup or when the schema changes.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="schemaFileName">
    /// The file name of the schema file.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder ExportSchemaOnStartup(
        this IRequestExecutorBuilder builder,
        string? schemaFileName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return InitializeOnStartup(builder, new RequestExecutorInitializationOptions
        {
            KeepWarm = true,
            WriteSchemaFile = new SchemaFileInitializationOptions
            {
                Enable = true,
                FileName = schemaFileName
            }
        });
    }
}
