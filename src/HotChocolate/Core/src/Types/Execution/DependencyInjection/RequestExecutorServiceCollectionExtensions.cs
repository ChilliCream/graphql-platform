using GreenDonut;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Requirements;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IRequestExecutorProvider"/> and related services
    /// to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGraphQLCore(this IServiceCollection services)
    {
        services.AddOptions();

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton<DefaultRequestContextAccessor>();
        services.TryAddSingleton<IRequestContextAccessor>(sp => sp.GetRequiredService<DefaultRequestContextAccessor>());
        services.TryAddSingleton<AggregateServiceScopeInitializer>();
        services.TryAddSingleton<ParameterBindingResolver>();

        services.TryAddSingleton(sp =>
        {
            var provider = sp.GetRequiredService<ObjectPoolProvider>();
            var policy = new StringBuilderPooledObjectPolicy();
            return provider.Create(policy);
        });

        // core services
        services
            .TryAddTypeConverter()
            .TryAddInputFormatter()
            .TryAddInputParser()
            .TryAddDefaultBatchDispatcher(default)
            .TryAddDefaultDataLoaderRegistry()
            .TryAddDataLoaderParameterExpressionBuilder()
            .AddSingleton<ResolverProvider>();

        // pools
        services
            .TryAddResolverTaskPool()
            .TryAddOperationContextPool()
            .TryAddSingleton<ObjectPool<DocumentValidatorContext>>(new DocumentValidatorContextPool());

        // global executor services
        services
            .TryAddVariableCoercion()
            .TryAddRequestExecutorResolver();

        // parser
        services.TryAddSingleton(
            sp =>
            {
                var modifiers = sp.GetService<IEnumerable<Action<RequestParserOptions>>?>();

                if (modifiers is null)
                {
                    return ParserOptions.Default;
                }

                var options = new RequestParserOptions();

                foreach (var configure in modifiers)
                {
                    configure(options);
                }

                return new ParserOptions(
                    noLocations: !options.IncludeLocations,
                    maxAllowedNodes: options.MaxAllowedNodes,
                    maxAllowedTokens: options.MaxAllowedTokens,
                    maxAllowedFields: options.MaxAllowedFields);
            });

        return services;
    }

    /// <summary>
    /// Adds the <see cref="IRequestExecutorProvider"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a named <see cref="IRequestExecutor"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="schemaName">
    /// The logical name of the <see cref="ISchemaDefinition"/> to configure.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure the executor.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQL(
        this IServiceCollection services,
        string? schemaName = null)
    {
        services.AddGraphQLCore();
        schemaName ??= ISchemaDefinition.DefaultName;
        return CreateBuilder(services, schemaName);
    }

    /// <summary>
    /// Adds the <see cref="IRequestExecutorProvider"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a named <see cref="IRequestExecutor"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="schemaName">
    /// The logical name of the <see cref="ISchemaDefinition"/> to configure.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure the executor.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQL(
        this IRequestExecutorBuilder builder,
        string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;
        return CreateBuilder(builder.Services, schemaName);
    }

    private static DefaultRequestExecutorBuilder CreateBuilder(
        IServiceCollection services,
        string schemaName)
    {
        var builder = new DefaultRequestExecutorBuilder(services, schemaName);

        builder.TryAddTypeInterceptor<DataLoaderRootFieldTypeInterceptor>();
        builder.TryAddTypeInterceptor<RequirementsTypeInterceptor>();

        builder.AddDocumentCache();

        if (!services.Any(t =>
            t.ServiceType == typeof(SchemaName)
            && t.ImplementationInstance is SchemaName s
            && s.Value.Equals(schemaName, StringComparison.Ordinal)))
        {
            services.AddSingleton(new SchemaName(schemaName));
        }

        builder.ConfigureSchemaServices(static s => s.TryAddSingleton<ITimeProvider, DefaultTimeProvider>());

        return builder;
    }

    public static IServiceCollection AddBatchDispatcher<T>(this IServiceCollection services)
        where T : class, IBatchDispatcher
    {
        services.RemoveAll<IBatchDispatcher>();
        services.AddScoped<IBatchDispatcher, T>();
        return services;
    }

    public static IServiceCollection AddBatchScheduler<T>(this IServiceCollection services)
        where T : class, IBatchScheduler
    {
        services.RemoveAll<IBatchScheduler>();
        services.AddScoped<IBatchScheduler, T>();
        return services;
    }

    /// <summary>
    /// Adds the batch dispatcher to the request executor.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="options">
    ///  The batch dispatcher options.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder AddDefaultBatchDispatcher(
        this IRequestExecutorBuilder builder,
        BatchDispatcherOptions options = default)
    {
        builder.Services.AddDefaultBatchDispatcher(options);
        return builder;
    }

    public static IServiceCollection AddDefaultBatchDispatcher(
        this IServiceCollection services,
        BatchDispatcherOptions options = default)
    {
        services.RemoveAll<IBatchScheduler>();
        services.TryAddDefaultBatchDispatcher(options);
        return services;
    }
}
