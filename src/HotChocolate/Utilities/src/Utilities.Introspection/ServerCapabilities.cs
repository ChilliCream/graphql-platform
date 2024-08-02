namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// Represents the capabilities that are supported by a GraphQL server.
/// </summary>
public class ServerCapabilities
{
    /// <summary>
    /// Gets a value that indicates whether the server supports the
    /// newer directive locations on when introspecting.
    /// </summary>
    public bool HasDirectiveLocations { get; internal set; }

    /// <summary>
    /// Gets a value that indicates whether the server supports the repeatable directives.
    /// </summary>
    public bool HasRepeatableDirectives { get; internal set;}

    /// <summary>
    /// Gets a value that indicates whether the server supports subscriptions.
    /// </summary>
    public bool HasSubscriptionSupport { get; internal set;}

    /// <summary>
    /// Gets a value that defines if the GraphQL server supports the @defer directive.
    /// </summary>
    public bool HasDeferSupport { get; internal set;}

    /// <summary>
    /// Gets a value that defines if the GraphQL server supports the @stream directive.
    /// </summary>
    public bool HasStreamSupport { get; internal set;}

    /// <summary>
    /// Gets a value that defines if the GraphQL server supports argument deprecation.
    /// </summary>
    public bool HasArgumentDeprecation { get; internal set;}

    /// <summary>
    /// Gets a value that defines if the GraphQL server supports schema descriptions.
    /// </summary>
    public bool HasSchemaDescription { get; internal set;}
}
