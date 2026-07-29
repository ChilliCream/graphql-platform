using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides the extension methods that connect a distributed application to Nitro.
/// </summary>
public static class NitroExtensions
{
    /// <summary>
    /// Adds GraphQL schema composition orchestration that composes against the fusion
    /// configurations of Nitro. A gateway that is configured with
    /// <see cref="WithNitroApiId{T}"/> composes the source schemas of the distributed
    /// application on top of the fusion configuration that Nitro serves for
    /// <paramref name="stage"/>, so it also serves the source schemas that run outside of the
    /// distributed application.
    /// </summary>
    /// <param name="builder">
    /// The distributed application builder.
    /// </param>
    /// <param name="stage">
    /// The name of the stage whose fusion configuration the gateways compose against. The settings
    /// of the source schemas that the fusion configuration carries resolve against this
    /// environment.
    /// </param>
    /// <returns>
    /// The distributed application builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Nitro is already added for another stage.
    /// </exception>
    /// <remarks>
    /// A source schema of the distributed application replaces the source schema of the same name
    /// in the fusion configuration. The name of a source schema that is declared with
    /// <see cref="GraphQLResourceBuilderExtensions.WithGraphQLSchemaEndpoint{T}"/> is the
    /// <c>name</c> of its settings file, while the name of a source schema that is declared with
    /// <see cref="GraphQLResourceBuilderExtensions.WithGraphQLSchemaFile{T}"/> is the configured
    /// source schema name or the name of the resource, which is not checked against its settings
    /// file. A source schema that ends up with another name is added to the composition instead of
    /// replacing the one in the fusion configuration.
    /// </remarks>
    public static IDistributedApplicationBuilder AddNitro(
        this IDistributedApplicationBuilder builder,
        string stage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        var options = SchemaCompositionRegistration.Ensure(builder);

        if (options.Coordinator is { } coordinator)
        {
            if (!string.Equals(coordinator.Stage, stage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Nitro is already added for the stage '{coordinator.Stage}'. A distributed "
                    + "application composes against a single stage, so AddNitro cannot be called "
                    + $"again for the stage '{stage}'.");
            }

            return builder;
        }

        options.Coordinator = NitroSeedCoordinator.CreateProduction(stage);

        return builder;
    }

    /// <summary>
    /// Selects the Nitro api that carries the fusion configuration of a gateway. The api id is the
    /// id that the Nitro dashboard and the Nitro CLI report for the api. Calling this method again
    /// replaces the previously configured api id.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a gateway.
    /// </param>
    /// <param name="apiId">
    /// The id of the Nitro api.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    /// <remarks>
    /// The api id only takes effect on a resource whose schema is composed and only when the
    /// distributed application calls <see cref="AddNitro"/>. On any other resource it is metadata
    /// without an effect.
    /// </remarks>
    public static IResourceBuilder<T> WithNitroApiId<T>(
        this IResourceBuilder<T> builder,
        string apiId)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);

        builder.WithAnnotation(
            new NitroApiIdAnnotation { ApiId = apiId },
            ResourceAnnotationMutationBehavior.Replace);

        return builder;
    }

    internal static string? GetNitroApiId(this IResource resource)
        => resource.Annotations.OfType<NitroApiIdAnnotation>().SingleOrDefault()?.ApiId;
}
