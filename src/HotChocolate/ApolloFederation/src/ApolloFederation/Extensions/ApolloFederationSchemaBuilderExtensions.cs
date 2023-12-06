using HotChocolate.ApolloFederation;
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
    public static ISchemaBuilder AddApolloFederation(
        this ISchemaBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        // disable hot chocolate tag directive
        // specify default Query type name if not specified
        builder.ModifyOptions(opt =>
        {
            opt.EnableTag = false;
            if (opt.QueryTypeName is null)
            {
                opt.QueryTypeName = "Query";
            }
        });

        builder.AddType<AnyType>();
        builder.AddType<EntityType>();
        builder.AddType(new ServiceType());
        builder.AddType<FieldSetType>();
        builder.AddType<ExtendsDirectiveType>();
        builder.AddType<ExternalDirectiveType>();
        builder.AddType<ProvidesDirectiveType>();
        builder.AddType<KeyDirectiveType>();
        builder.AddType<RequiresDirectiveType>();
        builder.TryAddTypeInterceptor<FederationTypeInterceptor>();
        return builder;
    }
}
