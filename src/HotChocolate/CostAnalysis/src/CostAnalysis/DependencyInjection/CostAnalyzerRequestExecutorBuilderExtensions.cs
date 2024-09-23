using HotChocolate;
using HotChocolate.CostAnalysis;
using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static class CostAnalyzerRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds the cost analyzer to the request pipeline.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to
    /// configure the GraphQL schema and request pipeline.
    /// </returns>
    public static IRequestExecutorBuilder AddCostAnalyzer(this IRequestExecutorBuilder builder)
    {
        builder.Services
            .AddSingleton<ICostMetricsCache, DefaultCostMetricsCache>();

        return builder
            .ConfigureSchemaServices(
                services =>
                {
                    services.TryAddEnumerable(
                        Singleton<ISchemaDocumentFormatter, CostSchemaDocumentFormatter>());

                    services.TryAddSingleton<CostOptions>(sp =>
                    {
                        var options = new CostOptions();

                        foreach (var configure in sp.GetServices<Action<CostOptions>>())
                        {
                            configure(options);
                        }

                        return options;
                    });
                })
            .AddDirectiveType<CostDirectiveType>()
            .AddDirectiveType<ListSizeDirectiveType>()
            .TryAddTypeInterceptor<CostTypeInterceptor>()

            // we are replacing the default pipeline if the cost analyzer is added.
            .Configure(c => c.DefaultPipelineFactory = AddDefaultPipeline);
    }

    /// <summary>
    /// Modify cost analyzer options.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to mutate the configuration object.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder ModifyCostOptions(
        this IRequestExecutorBuilder builder,
        Action<CostOptions> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.ConfigureSchemaServices(
            services => services.AddSingleton(configure));

        return builder;
    }

    /// <summary>
    /// Uses the cost analyzer middleware.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its
    /// execution.
    /// </returns>
    public static IRequestExecutorBuilder UseCostAnalyzer(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .AddCostAnalyzer()
            .UseRequest(CostAnalyzerMiddleware.Create());
    }

    internal static void AddDefaultPipeline(this IList<RequestCoreMiddleware> pipeline)
    {
        pipeline.Add(InstrumentationMiddleware.Create());
        pipeline.Add(ExceptionMiddleware.Create());
        pipeline.Add(TimeoutMiddleware.Create());
        pipeline.Add(DocumentCacheMiddleware.Create());
        pipeline.Add(DocumentParserMiddleware.Create());
        pipeline.Add(DocumentValidationMiddleware.Create());
        pipeline.Add(CostAnalyzerMiddleware.Create());
        pipeline.Add(OperationCacheMiddleware.Create());
        pipeline.Add(OperationResolverMiddleware.Create());
        pipeline.Add(OperationVariableCoercionMiddleware.Create());
        pipeline.Add(OperationExecutionMiddleware.Create());
    }
}
