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
    NotSupported,

    /// <summary>
    /// application/json
    /// </summary>
    Json,

    /// <summary>
    /// multipart/mixed
    /// </summary>
    MultiPart,

    /// <summary>
    /// application/graphql-response+json
    /// </summary>
    GraphQL
}
