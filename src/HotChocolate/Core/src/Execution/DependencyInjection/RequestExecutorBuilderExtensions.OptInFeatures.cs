using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder OptInFeatureStability(
        this IRequestExecutorBuilder builder,
        string feature,
        string stability)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (feature is null)
        {
            throw new ArgumentNullException(nameof(feature));
        }

        if (stability is null)
        {
            throw new ArgumentNullException(nameof(stability));
        }

        return Configure(
            builder,
            options => options.OnConfigureSchemaServicesHooks.Add(
                (ctx, _) => ctx.SchemaBuilder.AddSchemaConfiguration(
                    d => d.Directive(new OptInFeatureStabilityDirective(feature, stability)))));
    }
}
