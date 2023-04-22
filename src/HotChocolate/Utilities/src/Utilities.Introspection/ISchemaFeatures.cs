namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// Represents the features that are supported by a GraphQL server.
/// </summary>
public interface ISchemaFeatures
{
    /// <summary>
    /// Gets a value that indicates whether the server supports the
    /// newer directive locations on when introspecting.
    /// </summary>
    bool HasDirectiveLocations { get; }

    /// <summary>
    /// Gets a value that indicates whether the server supports the repeatable directives.
    /// </summary>
    bool HasRepeatableDirectives { get; }

    /// <summary>
    /// Gets a value that indicates whether the server supports subscriptions.
    /// </summary>
    bool HasSubscriptionSupport { get; }
}
