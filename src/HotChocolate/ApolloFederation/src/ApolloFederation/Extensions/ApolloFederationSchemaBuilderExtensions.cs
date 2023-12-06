using HotChocolate.ApolloFederation;
using ApolloAnyType = HotChocolate.ApolloFederation.AnyType;

namespace HotChocolate;

/// <summary>
/// Provides extensions to <see cref="ISchemaBuilder"/>.
/// </summary>
public static partial class ApolloFederationSchemaBuilderExtensions
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
    /// <param name="version">
    /// Target Federation version
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static ISchemaBuilder AddApolloFederation(
        this ISchemaBuilder builder,
        FederationVersion version = FederationVersion.Latest,
        Action<ISchemaTypeDescriptor>? schemaConfiguration = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.SetSchema(s =>
        {
            var link = FederationUtils.GetFederationLink(version);
            s.Link(link.Url, link.Import?.ToArray());
            schemaConfiguration?.Invoke(s);
        });
        return AddApolloFederationDefinitions(builder, version);
    }

    /// <summary>
    /// Adds support for Apollo Federation to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="schema">
    /// Federated schema object.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="schema"/> is <c>null</c>.
    /// </exception>
    public static ISchemaBuilder AddApolloFederation(this ISchemaBuilder builder, FederatedSchema schema)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        builder.SetSchema(schema);
        return AddApolloFederationDefinitions(builder, schema.FederationVersion);
    }

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
    private static ISchemaBuilder AddApolloFederationDefinitions(
        this ISchemaBuilder builder,
        FederationVersion version = FederationVersion.Latest)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Disable hot chocolate tag directive
        // specify default Query type name if not specified
        builder.ModifyOptions(opt =>
        {
            opt.EnableTag = false;
            opt.QueryTypeName ??= "Query";
        });

        // Scalars
        builder.AddType<ApolloAnyType>();
        builder.AddType<FieldSetType>();

        // Types
        builder.AddType<EntityType>();
        builder.AddType(new ServiceType(true));

        // Directives
        switch (version)
        {
            case FederationVersion.Federation20:
            {
                builder.AddType<ExtendsDirectiveType>();
                builder.AddType<ExternalDirectiveType>();
                builder.AddType<InaccessibleDirectiveType>();
                builder.AddType<KeyDirectiveType>();
                builder.AddType<LinkDirectiveType>();
                builder.AddType<OverrideDirectiveType>();
                builder.AddType<ProvidesDirectiveType>();
                builder.AddType<RequiresDirectiveType>();
                builder.AddType<ShareableDirectiveType>();
                builder.AddType<TagDirectiveType>();
                break;
            }

            case FederationVersion.Federation21:
            case FederationVersion.Federation22:
            {
                builder.AddType<ComposeDirectiveType>();
                goto case FederationVersion.Federation20;
            }

            case FederationVersion.Federation23:
            case FederationVersion.Federation24:
            {
                builder.AddType<InterfaceObjectDirectiveType>();
                goto case FederationVersion.Federation22;
            }

            case FederationVersion.Federation25:
            {
                builder.AddType<ScopeType>();
                builder.AddType<AuthenticatedDirectiveType>();
                builder.AddType<RequiresScopesDirectiveType>();
                builder.BindRuntimeType<Scope, ScopeType>();
                goto case FederationVersion.Federation24;
            }

            default:
            {
                break;
            }
        }

        builder.TryAddTypeInterceptor<FederationTypeInterceptor>();
        return builder;
    }
}
