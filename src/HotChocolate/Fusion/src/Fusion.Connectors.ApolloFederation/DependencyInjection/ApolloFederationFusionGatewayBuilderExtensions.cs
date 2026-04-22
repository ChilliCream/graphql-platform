using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Connectors.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the Apollo Federation connector on
/// <see cref="IFusionGatewayBuilder"/>.
/// </summary>
public static class ApolloFederationFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds Apollo Federation support to the Fusion gateway. Source schemas whose
    /// settings carry an <c>extensions.apolloFederation</c> block will be served by
    /// the Apollo Federation <c>_entities</c> connector.
    /// </summary>
    /// <param name="builder">The Fusion gateway builder.</param>
    /// <returns>The Fusion gateway builder for chaining.</returns>
    public static IFusionGatewayBuilder AddApolloFederationSupport(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ISourceSchemaClientFactory, ApolloFederationSourceSchemaClientFactory>());

        return FusionSetupUtilities.Configure(
            builder,
            static setup =>
            {
                if (!setup.SourceSchemaClientConfigurationParsers.Any(
                    static p => p is ApolloFederationClientConfigurationParser))
                {
                    setup.SourceSchemaClientConfigurationParsers.Add(
                        new ApolloFederationClientConfigurationParser());
                }
            });
    }
}
