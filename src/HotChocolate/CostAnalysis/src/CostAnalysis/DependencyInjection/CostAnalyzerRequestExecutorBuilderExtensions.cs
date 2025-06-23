using HotChocolate;
using HotChocolate.CostAnalysis;
using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Types;
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
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureSchemaServices(
                static services =>
                {
                    services.TryAddSingleton<ICostMetricsCache, DefaultCostMetricsCache>();

                    services.TryAddEnumerable(
                        Singleton<ISchemaDocumentFormatter, CostSchemaDocumentFormatter>());

                    services.TryAddSingleton(sp =>
                    {
                        var options = new CostOptions();

                        foreach (var configure in sp.GetServices<Action<CostOptions>>())
                        {
                            configure(options);
                        }

                        return options;
                    });

                    services.TryAddSingleton(sp =>
                    {
                        var requestOptions = sp.GetRequiredService<CostOptions>();
                        return new RequestCostOptions(
                            requestOptions.MaxFieldCost,
                            requestOptions.MaxTypeCost,
                            requestOptions.EnforceCostLimits,
                            requestOptions.SkipAnalyzer,
                            requestOptions.Filtering.VariableMultiplier);
                    });
                })
            .AddDirectiveType<CostDirectiveType>()
            .AddDirectiveType<ListSizeDirectiveType>()
            .TryAddTypeInterceptor<CostTypeInterceptor>()
            .TryAddTypeInterceptor<CostDirectiveTypeInterceptor>()
            .AppendUseRequest(
                after: CommonMiddleware.DocumentValidationKey,
                configuration: CostAnalyzerMiddleware.Create(),
                allowMultiple: false);
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.ConfigureSchemaServices(
            services => services.AddSingleton(configure));

        return builder;
    }
}
