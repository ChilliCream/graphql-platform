namespace HotChocolate.AspNetCore;

/// <summary>
/// Specifies the GraphQL request content types that hot chocolate supports.
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
    Form,
}
