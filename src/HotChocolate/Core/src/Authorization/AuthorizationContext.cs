using HotChocolate.Language;

namespace HotChocolate.Authorization;

/// <summary>
/// Represents the state that is used to execute authorization policies.
/// </summary>
public sealed class AuthorizationContext
{
    /// <summary>
    /// Initializes a new <see cref="AuthorizationContext"/>.
    /// </summary>
    /// <param name="schema">The GraphQL schema.</param>
    /// <param name="services">The application services.</param>
    /// <param name="contextData">The request context data.</param>
    /// <param name="document">The GraphQL request document.</param>
    /// <param name="documentId">A unique string identifying the GraphQL document</param>
    public AuthorizationContext(
        ISchema schema,
        IServiceProvider services,
        IDictionary<string, object?> contextData,
        DocumentNode document,
        string documentId)
    {
        Schema = schema;
        Services = services;
        ContextData = contextData;
        Document = document;
        DocumentId = documentId;
    }

    /// <summary>
    /// Gets the GraphQL schema.
    /// </summary>
    public ISchema Schema { get; }

    /// <summary>
    /// Gets the application services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the request context data.
    /// </summary>
    public IDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets a unique string identifying the GraphQL request document.
    /// </summary>
    public string DocumentId { get; }
}
