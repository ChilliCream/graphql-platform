namespace HotChocolate.AspNetCore;

/// <summary>
/// Specifies the GraphQL response content types that hot chocolate supports.
/// </summary>
public enum ResponseContentType
{
    /// <summary>
    /// Not supported content-type.
    /// </summary>
    Unknown,

    /// <summary>
    /// application/json
    /// </summary>
    Json,

    /// <summary>
    /// multipart/mixed
    /// </summary>
    MultiPartMixed,

    /// <summary>
    /// application/graphql-response+json
    /// </summary>
    GraphQLResponse,

    /// <summary>
    /// application/graphql-response+jsonl
    /// </summary>
    GraphQLResponseStream,

    /// <summary>
    /// text/event-stream
    /// </summary>
    EventStream,

    /// <summary>
    /// application/jsonl
    /// </summary>
    JsonLines
}
