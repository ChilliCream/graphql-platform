namespace HotChocolate.AspNetCore;

/// <summary>
/// Specifies the GraphQL request content-type.
/// </summary>
public enum RequestContentType
{
    /// <summary>
    /// Not specified.
    /// </summary>
    None,

    /// <summary>
    /// application/json
    /// </summary>
    Json,

    /// <summary>
    /// multipart/mixed
    /// </summary>
    Form
}

/// <summary>
/// Specifies the GraphQL response content-type.
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
    /// text/event-stream
    /// </summary>
    EventStream
}
