using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Authorization;

/// <summary>
/// Represents the state used to execute authorization policies.
/// </summary>
public sealed class AuthorizationContext : IFeatureProvider
{
    /// <summary>
    /// Initializes a new <see cref="AuthorizationContext"/>.
    /// </summary>
    /// <param name="schema">The GraphQL schema.</param>
    /// <param name="services">The application services.</param>
    /// <param name="contextData">The request context data.</param>
    /// <param name="features">The request feature collection.</param>
    /// <param name="document">The GraphQL request document.</param>
    /// <param name="documentId">A unique string identifying the GraphQL document</param>
    public AuthorizationContext(
        ISchemaDefinition schema,
        IServiceProvider services,
        IDictionary<string, object?> contextData,
        IFeatureCollection features,
        DocumentNode document,
        OperationDocumentId documentId)
    {
        Schema = schema;
        Services = services;
        ContextData = contextData;
        Features = features;
        Document = document;
        DocumentId = documentId;
    }

    /// <summary>
    /// Gets the GraphQL schema.
    /// </summary>
    public ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the application services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the request context data.
    /// </summary>
    public IDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets a unique string identifying the GraphQL request document.
    /// </summary>
    public OperationDocumentId DocumentId { get; }
}
