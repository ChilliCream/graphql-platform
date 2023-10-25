using HotChocolate.ApolloFederation;
using AnyType = HotChocolate.ApolloFederation.AnyType;

namespace HotChocolate;

/// <summary>
/// Provides extensions to <see cref="ISchemaBuilder"/>.
/// </summary>
public static class ApolloFederationSchemaBuilderExtensions
{
    /// <summary>
    /// Adds support for Apollo Federation to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    internal static ISchemaBuilder AddApolloFederation(
        this ISchemaBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddType<AnyType>();
        builder.AddType<EntityType>();
        builder.AddType<ServiceType>();
        builder.AddType<FieldSetType>();
        builder.AddType<ExternalDirectiveType>();
        builder.AddType<ProvidesDirectiveType>();
        builder.AddType<KeyDirectiveType>();
        builder.AddType<RequiresDirectiveType>();
        builder.TryAddTypeInterceptor<FederationTypeInterceptor>();
        return builder;
    }
}
