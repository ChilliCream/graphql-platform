namespace HotChocolate.AspNetCore;

/// <summary>
/// The default HTTP fetch method for Banana Cake Pop.
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
