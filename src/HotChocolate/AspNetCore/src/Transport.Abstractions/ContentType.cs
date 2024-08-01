namespace HotChocolate.Transport;

/// <summary>
/// This class provides the default content types for GraphQL requests and responses.
/// </summary>
public static class ContentType
{
    /// <summary>
    /// Gets the application/json content type.
    /// </summary>
    public const string Json = "application/json";

    /// <summary>
    /// Gets the application/graphql-response+json content type.
    /// </summary>
    public const string GraphQL = "application/graphql-response+json";

    /// <summary>
    /// Gets the text/event-stream content type.
    /// </summary>
    public const string EventStream = "text/event-stream";
}
