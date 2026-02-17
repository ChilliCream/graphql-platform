using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds an opt-in feature stability directive to the schema,
    /// allowing you to specify the stability level of a feature.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="feature">
    /// The name of the feature for which to set the stability.
    /// </param>
    /// <param name="stability">
    /// The stability level of the feature.
    /// </param>
    /// <returns>
    /// The <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="feature"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stability"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder OptInFeatureStability(
        this IRequestExecutorBuilder builder,
        string feature,
        string stability)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(stability);

        return builder.Configure(options => options.OnConfigureSchemaServicesHooks.Add(
            (ctx, _) => ctx.SchemaBuilder.AddSchemaConfiguration(
                d => d.Directive(new OptInFeatureStabilityDirective(feature, stability)))));
    }
}
