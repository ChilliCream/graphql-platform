using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Directives;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

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
            .UseOperationCache()
            .UseCostAnalysis()
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

        builder.ConfigureSchema(
            (serviceProvider, _) =>
            {
                var options = serviceProvider.GetRequiredService<CostAnalysisOptions>();

                configure(options);
            });

        return builder;
    }

    internal static IRequestExecutorBuilder AddCostAnalysis(this IRequestExecutorBuilder builder)
    {
        builder.Services
            .AddSingleton<CostAnalysisOptions>()
            .AddSingleton<ICostMetricsCache, DefaultCostMetricsCache>();

        return builder
            .AddDirectiveType<CostDirectiveType>()
            .AddDirectiveType<ListSizeDirectiveType>()
            .AddType<CostByLocationType>()
            .AddType<CostCountTypeType>()
            .AddType<CostMetricsType>()
            .AddType<CostType>()
            .TryAddTypeInterceptor<CostIntrospectionTypeInterceptor>();
    }
}
