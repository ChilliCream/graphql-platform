namespace HotChocolate.AspNetCore;

/// <summary>
/// The default HTTP fetch method for Nitro.
/// </summary>
public enum DefaultHttpMethod
{
    /// <summary>
    /// Use a GraphQL HTTP GET request.
    /// </summary>
    Get,

    /// <summary>
    /// Use a GraphQL HTTP Post request.
    /// </summary>
    Post,
}
