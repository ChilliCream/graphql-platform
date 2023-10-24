namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// Represents the features that are supported by a GraphQL server.
/// </summary>
public class SchemaFeatures
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

    public bool HasDeferSupport { get; internal set;}

    public bool HasStreamSupport { get; internal set;}

    public bool HasArgumentDeprecation { get; internal set;}
    
    public bool HasSchemaDescription { get; internal set;}
}