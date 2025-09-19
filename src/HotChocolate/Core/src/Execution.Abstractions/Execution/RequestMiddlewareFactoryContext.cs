using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// This context is available when creating a middleware pipeline.
/// </summary>
public class RequestMiddlewareFactoryContext : IFeatureProvider
{
    private IServiceProvider? _schemaServices;

    /// <summary>
    /// Gets the GraphQL schema name.
    /// </summary>
    public required ISchemaDefinition Schema { get; init; }

    /// <summary>
    /// Gets the application level service provider.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets access to the schema services.
    /// </summary>
    public IServiceProvider SchemaServices
    {
        get
        {
            if (_schemaServices is null)
            {
                // this looks complicated as there is a service property on the schema.
                // But in the case that the schema has no services, we will use the application level service provider.
                var schemaServiceAccessor = Features.Get<SchemaServicesProviderAccessor>();
                _schemaServices = schemaServiceAccessor is not null ? schemaServiceAccessor.Services : Services;
            }

            return _schemaServices;
        }
    }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features { get; } = new FeatureCollection();
}
