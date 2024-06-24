using HotChocolate.CostAnalysis;
using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Uses the default pipeline with the addition of the cost analysis middleware.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its
    /// execution.
    /// </returns>
    public static IRequestExecutorBuilder UseDefaultPipelineWithCostAnalysis(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseCostAnalysis()
            .UseOperationCache()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseOperationExecution();
    }

    /// <summary>
    /// Uses the cost analysis middleware.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its
    /// execution.
    /// </returns>
    public static IRequestExecutorBuilder UseCostAnalysis(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .AddCostAnalysis()
            .UseRequest(CostAnalysisMiddleware.Create());
    }

    /// <summary>
    /// Modify cost analysis options.
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
    public static IRequestExecutorBuilder ModifyCostAnalysisOptions(
        this IRequestExecutorBuilder builder,
        Action<CostAnalysisOptions> configure)
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

    internal static IRequestExecutorBuilder AddCostAnalysis(this IRequestExecutorBuilder builder)
    {
        builder.Services
            .AddSingleton<ICostMetricsCache, DefaultCostMetricsCache>();

        return builder
            .ConfigureSchemaServices(
                services =>
                {
                    services.TryAddSingleton<CostAnalysisOptions>(sp =>
                    {
                        var options = new CostAnalysisOptions();

                        foreach (var configure in sp.GetServices<Action<CostAnalysisOptions>>())
                        {
                            configure(options);
                        }

                        return options;
                    });
                })
            .AddDirectiveType<CostDirectiveType>()
            .AddDirectiveType<ListSizeDirectiveType>();
    }
}
