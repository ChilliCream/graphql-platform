using HotChocolate.ApolloFederation;
using ApolloAnyType = HotChocolate.ApolloFederation.AnyType;

namespace HotChocolate;

// TODO : This we need to completly rewrite.
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
    /// <param name="schemaConfiguration"></param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static ISchemaBuilder AddApolloFederation(
        this ISchemaBuilder builder,
        FederationVersion version = FederationVersion.Latest,
        Action<ISchemaTypeDescriptor>? schemaConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // TODO : we will move this to the type interceptor.
        builder.SetSchema(s =>
        {
            var link = FederationUtils.GetFederationLink(version);
            s.Link(link.Url, link.Import);
            schemaConfiguration?.Invoke(s);
        });
        AddApolloFederationDefinitions(builder, version);
        return builder;
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(schema);

        builder.SetSchema(schema);
        AddApolloFederationDefinitions(builder, schema.FederationVersion);
        return builder;
    }

    /// <summary>
    /// Adds support for Apollo Federation to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="version"></param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    private static void AddApolloFederationDefinitions(
        this ISchemaBuilder builder,
        FederationVersion version = FederationVersion.Latest)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.TryAddTypeInterceptor<FederationTypeInterceptor>();

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
                builder.AddType<ExtendServiceTypeDirectiveType>();
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

            case FederationVersion.Federation26:
            {
                builder.AddType<PolicyDirectiveType>();
                goto case FederationVersion.Federation25;
            }

            default:
            {
                break;
            }
        }

        builder.TryAddTypeInterceptor<FederationTypeInterceptor>();
    }
}
